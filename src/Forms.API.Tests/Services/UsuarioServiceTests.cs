using System.Security.Claims;
using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Forms.API.Tests.Services;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _usuarioRepositoryMock = new Mock<IUsuarioRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _service = new UsuarioService(_usuarioRepositoryMock.Object, _httpContextAccessorMock.Object);
    }

    private void SetHttpContextUser(string userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
    }

    [Fact]
    public async Task AddUsuario_QuandoEmailForInvalido_LancaArgumentException()
    {
        // Arrange
        var usuarioDto = new UsuarioDTO
        {
            Nome = "Teste",
            Email = "email-invalido",
            Senha = "123",
            Perfil = PerfilUsuario.Comum
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddUsuario(usuarioDto));
        Assert.Equal("Email inválido.", ex.Message);
        _usuarioRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UsuarioModel>()), Times.Never);
    }

    [Fact]
    public async Task AddUsuario_QuandoEmailJaExistir_LancaArgumentException()
    {
        // Arrange
        var usuarioDto = new UsuarioDTO
        {
            Nome = "Teste",
            Email = "existente@test.com",
            Senha = "123",
            Perfil = PerfilUsuario.Comum
        };

        var existingUser = new UsuarioModel
        {
            Id = 1,
            Nome = "Existente",
            Email = "existente@test.com",
            Senha = "hash",
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByEmailAsync(usuarioDto.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddUsuario(usuarioDto));
        Assert.Equal("Email já cadastrado.", ex.Message);
        _usuarioRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UsuarioModel>()), Times.Never);
    }

    [Fact]
    public async Task AddUsuario_QuandoValido_AdicionaUsuarioERetornaRespostaDeSucesso()
    {
        // Arrange
        var usuarioDto = new UsuarioDTO
        {
            Nome = "Novo Usuário",
            Email = "novo@test.com",
            Senha = "PlainPassword123!",
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByEmailAsync(usuarioDto.Email))
            .ReturnsAsync((UsuarioModel?)null);

        _usuarioRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UsuarioModel>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddUsuario(usuarioDto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Usuário adicionado com sucesso.", result.Message);
        _usuarioRepositoryMock.Verify(r => r.AddAsync(It.Is<UsuarioModel>(u =>
            u.Nome == usuarioDto.Nome &&
            u.Email == usuarioDto.Email &&
            u.Perfil == usuarioDto.Perfil &&
            BCrypt.Net.BCrypt.Verify(usuarioDto.Senha, u.Senha))), Times.Once);
    }

    [Fact]
    public async Task GetUsuarioById_QuandoUsuarioNaoForDonoNemAdmin_LancaKeyNotFoundException()
    {
        // Arrange
        SetHttpContextUser("2", PerfilUsuario.Comum.ToString());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetUsuarioById(1));
        Assert.Equal("Objeto não encontrado.", ex.Message);
    }

    [Fact]
    public async Task GetUsuarioById_QuandoUsuarioForDono_RetornaUsuario()
    {
        // Arrange
        int userId = 1;
        SetHttpContextUser(userId.ToString(), PerfilUsuario.Comum.ToString());

        var expectedUser = new UsuarioModel
        {
            Id = userId,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "hash",
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUsuarioById(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUsuarioById_QuandoUsuarioForAdmin_RetornaUsuario()
    {
        // Arrange
        int targetUserId = 1;
        SetHttpContextUser("999", PerfilUsuario.Administrador.ToString());

        var expectedUser = new UsuarioModel
        {
            Id = targetUserId,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "hash",
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByIdAsync(targetUserId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUsuarioById(targetUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetUserId, result.Id);
    }

    [Fact]
    public async Task GetAllUsuarios_QuandoChamado_RetornaTodosOsUsuarios()
    {
        // Arrange
        var list = new List<UsuarioModel>
        {
            new() { Id = 1, Nome = "U1", Email = "u1@test.com", Senha = "pwd", Perfil = PerfilUsuario.Comum },
            new() { Id = 2, Nome = "U2", Email = "u2@test.com", Senha = "pwd", Perfil = PerfilUsuario.Administrador }
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(list);

        // Act
        var result = await _service.GetAllUsuarios();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateUsuario_QuandoEmailForInvalido_LancaArgumentException()
    {
        // Arrange
        int userId = 1;
        var existingUser = new UsuarioModel
        {
            Id = userId,
            Nome = "Antigo",
            Email = "antigo@test.com",
            Senha = BCrypt.Net.BCrypt.HashPassword("123"),
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        var updateDto = new UsuarioDTO
        {
            Nome = "Novo",
            Email = "email-invalido",
            Senha = "123",
            Perfil = PerfilUsuario.Comum
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateUsuario(updateDto, userId));
        Assert.Equal("Email inválido.", ex.Message);
        _usuarioRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UsuarioModel>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUsuario_QuandoSenhaForAlterada_GeraHashDaNovaSenhaEAtualiza()
    {
        // Arrange
        int userId = 1;
        string oldPassword = "OldPassword123!";
        string newPassword = "NewPassword123!";
        var existingUser = new UsuarioModel
        {
            Id = userId,
            Nome = "Antigo",
            Email = "antigo@test.com",
            Senha = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Perfil = PerfilUsuario.Comum
        };

        _usuarioRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _usuarioRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<UsuarioModel>()))
            .Returns(Task.CompletedTask);

        var updateDto = new UsuarioDTO
        {
            Nome = "Nome Atualizado",
            Email = "novo@test.com",
            Senha = newPassword,
            Perfil = PerfilUsuario.Administrador
        };

        // Act
        var result = await _service.UpdateUsuario(updateDto, userId);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Usuário atualizado com sucesso.", result.Message);
        _usuarioRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UsuarioModel>(u =>
            u.Nome == "Nome Atualizado" &&
            u.Email == "novo@test.com" &&
            u.Perfil == PerfilUsuario.Administrador &&
            BCrypt.Net.BCrypt.Verify(newPassword, u.Senha))), Times.Once);
    }

    [Fact]
    public async Task DeleteUsuario_QuandoChamado_ChamaDeleteDoRepositorio()
    {
        // Arrange
        int userId = 1;
        _usuarioRepositoryMock
            .Setup(r => r.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteUsuario(userId);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Usuário deletado com sucesso.", result.Message);
        _usuarioRepositoryMock.Verify(r => r.DeleteAsync(userId), Times.Once);
    }
}
