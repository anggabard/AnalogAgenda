using AnalogAgenda.Server.Controllers;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace AnalogAgenda.Server.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _controller = new AccountController(_mockTableService.Object);
        
        // Setup HttpContext with authentication services
        var services = new ServiceCollection();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        
        mockServiceProvider.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(mockAuthService.Object);
        
        mockAuthService.Setup(x => x.SignInAsync(
            It.IsAny<HttpContext>(), 
            It.IsAny<string>(), 
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = mockServiceProvider.Object
        };
        
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }


    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(new List<UserEntity>());

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Bad creds", unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var hashedPassword = PasswordHasher.HashPassword("correctpassword");
        var user = new UserEntity
        {
            Email = "test@example.com",
            PasswordHash = hashedPassword,
            Name = "Test User"
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync([user]);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Bad creds", unauthorizedResult.Value);
    }

    [Fact]
    public void Me_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = _controller.Me();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Me_WhenAuthenticated_ReturnsIdentityDto()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.Me();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var identityDto = Assert.IsType<IdentityDto>(okResult.Value);
        Assert.Equal("Test User", identityDto.Username);
        Assert.Equal("test@example.com", identityDto.Email);
    }

}
