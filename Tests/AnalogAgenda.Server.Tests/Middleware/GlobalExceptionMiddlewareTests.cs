using AnalogAgenda.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AnalogAgenda.Server.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;

    public GlobalExceptionMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(new ArgumentException("Invalid argument"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(new KeyNotFoundException("Resource not found"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnknownException_ReturnsInternalServerError()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(new Exception("Unknown error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenException_LogsError()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = "GET";
        context.Request.Path = "/test";

        var exception = new Exception("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
