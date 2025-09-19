using AnalogAgenda.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace AnalogAgenda.Server.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;

    public RateLimitingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_WithNonAuthEndpoint_CallsNext()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/notes";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthEndpointUnderLimit_CallsNext()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/account/login";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Response.Body = new MemoryStream();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

}
