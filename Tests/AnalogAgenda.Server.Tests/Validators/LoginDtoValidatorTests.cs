using AnalogAgenda.Server.Validators;
using Database.DTOs;
using Xunit;

namespace AnalogAgenda.Server.Tests.Validators;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator;

    public LoginDtoValidatorTests()
    {
        _validator = new LoginDtoValidator();
    }

    [Fact]
    public void Validate_WithValidLoginDto_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidEmail_ShouldHaveValidationError(string email)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = email,
            Password = "password123"
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage == "Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = email,
            Password = "password123"
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage == "Please provide a valid email address");
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longEmail = new string('a', 95) + "@test.com"; // 104 characters total
        var loginDto = new LoginDto
        {
            Email = longEmail,
            Password = "password123"
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage == "Email cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidPassword_ShouldHaveValidationError(string password)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password" && e.ErrorMessage == "Password is required");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("12345")]
    public void Validate_WithPasswordTooShort_ShouldHaveValidationError(string password)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password" && e.ErrorMessage == "Password must be at least 6 characters long");
    }

    [Fact]
    public void Validate_WithPasswordTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longPassword = new string('a', 101); // 101 characters
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = longPassword
        };

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password" && e.ErrorMessage == "Password cannot exceed 100 characters");
    }

}