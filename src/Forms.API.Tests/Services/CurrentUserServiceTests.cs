using System.Security.Claims;
using Forms.API.Enums;
using Forms.API.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Forms.API.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _service = new CurrentUserService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void GetCurrentUserId_WhenNameIdentifierClaimExists_ReturnsParsedUserId()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "42") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetCurrentUserId_WhenClaimOrHttpContextMissing_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        // Act e Assert
        Assert.Throws<UnauthorizedAccessException>(() => _service.GetCurrentUserId());

    }

    [Fact]
    public void GetCurrentUserPerfil_WhenRoleClaimExists_ReturnsParsedPerfilUsuario()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Role, PerfilUsuario.Administrador.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserPerfil();

        // Assert
        Assert.Equal(PerfilUsuario.Administrador, result);
    }

    [Fact]
    public void GetCurrentUserPerfil_WhenRoleClaimMissing_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal() };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        // Act & Assert
        var ex = Assert.Throws<UnauthorizedAccessException>(() => _service.GetCurrentUserPerfil());
        Assert.Equal("Necessário realizar o login para continuar.", ex.Message);
    }
}
