using Forms.API.Controllers;
using Forms.API.DTOs;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forms.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResultWithResponseDTO()
    {
        // Arrange
        var loginDto = new LoginDTO { Email = "user@test.com", Senha = "Password123!" };
        var expectedResponse = new ResponseDTO { Status = true, Message = "Bearer: valid_token" };

        _authServiceMock
            .Setup(s => s.Login(loginDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Bearer: valid_token", response.Message);
        _authServiceMock.Verify(s => s.Login(loginDto), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequestResult()
    {
        // Arrange
        var loginDto = new LoginDTO { Email = "user@test.com", Senha = "WrongPassword" };
        var expectedResponse = new ResponseDTO { Status = false, Message = "Credenciais inválidas." };

        _authServiceMock
            .Setup(s => s.Login(loginDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(badRequestResult.Value);
        Assert.False(response.Status);
        Assert.Equal("Credenciais inválidas.", response.Message);
        _authServiceMock.Verify(s => s.Login(loginDto), Times.Once);
    }
}
