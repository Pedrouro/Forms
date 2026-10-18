using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Implementations;
using Moq;

namespace Forms.API.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock;
    private readonly AuthService _service;
    private const string ValidJwtKey = "chave-super-secreta-para-testes-unitarios-com-mais-de-256-bits!";

    public AuthServiceTests()
    {
        _usuarioRepositoryMock = new Mock<IUsuarioRepository>();
        _service = new AuthService(_usuarioRepositoryMock.Object);
        Environment.SetEnvironmentVariable("JWT_KEY", ValidJwtKey);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", null);
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsFalseWithInvalidCredentialsMessage()
    {
        // Arrange
        var loginDto = new LoginDTO { Email = "naoexiste@test.com", Senha = "123" };
        _usuarioRepositoryMock
            .Setup(r => r.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync((UsuarioModel?)null);

        // Act
        var result = await _service.Login(loginDto);

        // Assert
        Assert.False(result.Status);
        Assert.Equal("Credenciais inválidas.", result.Message);
    }

    [Fact]
    public async Task Login_WhenPasswordDoesNotMatch_ReturnsFalseWithInvalidCredentialsMessage()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var usuario = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = passwordHash,
            Perfil = PerfilUsuario.Comum
        };
        var loginDto = new LoginDTO { Email = "pedro@test.com", Senha = "WrongPassword" };

        _usuarioRepositoryMock
            .Setup(r => r.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync(usuario);

        // Act
        var result = await _service.Login(loginDto);

        // Assert
        Assert.False(result.Status);
        Assert.Equal("Credenciais inválidas.", result.Message);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTrueWithBearerToken()
    {
        // Arrange
        string rawPassword = "ValidPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);
        var usuario = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = passwordHash,
            Perfil = PerfilUsuario.Administrador
        };
        var loginDto = new LoginDTO { Email = "pedro@test.com", Senha = rawPassword };

        _usuarioRepositoryMock
            .Setup(r => r.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync(usuario);

        // Act
        var result = await _service.Login(loginDto);

        // Assert
        Assert.True(result.Status);
        Assert.StartsWith("Bearer: ", result.Message);
    }

    [Fact]
    public void GerarToken_WhenJwtKeyIsMissing_ThrowsException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("JWT_KEY", null);
        var usuario = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "hash",
            Perfil = PerfilUsuario.Comum
        };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => _service.GerarToken(usuario));
        Assert.Equal("JWT_key não encontrada.", ex.Message);
    }

    [Fact]
    public void GerarToken_WhenJwtKeyIsValid_ReturnsTokenString()
    {
        // Arrange
        var usuario = new UsuarioModel
        {
            Id = 2,
            Nome = "Admin",
            Email = "admin@test.com",
            Senha = "hash",
            Perfil = PerfilUsuario.Administrador
        };

        // Act
        var token = _service.GerarToken(usuario);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }
}
