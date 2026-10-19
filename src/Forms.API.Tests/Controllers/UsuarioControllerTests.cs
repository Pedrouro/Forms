using Forms.API.Controllers;
using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Models;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forms.API.Tests.Controllers;

public class UsuarioControllerTests
{
    private readonly Mock<IUsuarioService> _usuarioServiceMock;
    private readonly UsuarioController _controller;

    public UsuarioControllerTests()
    {
        _usuarioServiceMock = new Mock<IUsuarioService>();
        _controller = new UsuarioController(_usuarioServiceMock.Object);
    }

    [Fact]
    public async Task CreateUsuario_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        var usuarioDto = new UsuarioDTO
        {
            Nome = "Test User",
            Email = "test@example.com",
            Senha = "Password123!",
            Perfil = PerfilUsuario.Comum
        };
        var expectedResponse = new ResponseDTO { Status = true, Message = "Usuário adicionado com sucesso." };

        _usuarioServiceMock
            .Setup(s => s.AddUsuario(usuarioDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateUsuario(usuarioDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Usuário adicionado com sucesso.", response.Message);
        _usuarioServiceMock.Verify(s => s.AddUsuario(usuarioDto), Times.Once);
    }

    [Fact]
    public async Task GetUsuario_ComIdValido_RetornaOkResultComUsuarioModel()
    {
        // Arrange
        int userId = 1;
        var expectedUser = new UsuarioModel
        {
            Id = userId,
            Nome = "Test User",
            Email = "test@example.com",
            Senha = "hashed_password",
            Perfil = PerfilUsuario.Comum
        };

        _usuarioServiceMock
            .Setup(s => s.GetUsuarioById(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUsuario(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UsuarioModel>(okResult.Value);
        Assert.Equal(userId, user.Id);
        Assert.Equal("Test User", user.Nome);
        _usuarioServiceMock.Verify(s => s.GetUsuarioById(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllUsuarios_QuandoChamado_RetornaOkResultComListaDeUsuarios()
    {
        // Arrange
        var usersList = new List<UsuarioModel>
        {
            new() { Id = 1, Nome = "User 1", Email = "user1@test.com", Senha = "pwd1", Perfil = PerfilUsuario.Comum },
            new() { Id = 2, Nome = "User 2", Email = "user2@test.com", Senha = "pwd2", Perfil = PerfilUsuario.Administrador }
        };

        _usuarioServiceMock
            .Setup(s => s.GetAllUsuarios())
            .ReturnsAsync(usersList);

        // Act
        var result = await _controller.GetAllUsuarios();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IEnumerable<UsuarioModel>>(okResult.Value);
        Assert.Equal(2, users.Count());
        _usuarioServiceMock.Verify(s => s.GetAllUsuarios(), Times.Once);
    }

    [Fact]
    public async Task DeleteUsuario_ComIdValido_RetornaOkResultComMensagem()
    {
        // Arrange
        int userId = 1;
        var response = new ResponseDTO { Status = true, Message = "Usuário deletado com sucesso." };

        _usuarioServiceMock
            .Setup(s => s.DeleteUsuario(userId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.DeleteUsuario(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var message = Assert.IsType<string>(okResult.Value);
        Assert.Equal("Usuário deletado com sucesso.", message);
        _usuarioServiceMock.Verify(s => s.DeleteUsuario(userId), Times.Once);
    }

    [Fact]
    public async Task UpdateUsuario_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int userId = 1;
        var usuarioDto = new UsuarioDTO
        {
            Nome = "Updated Name",
            Email = "updated@example.com",
            Senha = "NewPassword123!",
            Perfil = PerfilUsuario.Comum
        };
        var expectedResponse = new ResponseDTO { Status = true, Message = "Usuário atualizado com sucesso." };

        _usuarioServiceMock
            .Setup(s => s.UpdateUsuario(usuarioDto, userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateUsuario(usuarioDto, userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Usuário atualizado com sucesso.", response.Message);
        _usuarioServiceMock.Verify(s => s.UpdateUsuario(usuarioDto, userId), Times.Once);
    }
}
