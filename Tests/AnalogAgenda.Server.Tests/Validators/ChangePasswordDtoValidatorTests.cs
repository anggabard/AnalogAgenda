using AnalogAgenda.Server.Validators;
using Database.DTOs;

namespace AnalogAgenda.Server.Tests.Validators;

public class ChangePasswordDtoValidatorTests
{
    private readonly ChangePasswordDtoValidator _validator;

    public ChangePasswordDtoValidatorTests()
    {
        _validator = new ChangePasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidChangePasswordDto_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = "oldpassword123",
            NewPassword = "newpassword456"
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidOldPassword_ShouldHaveValidationError(string? oldPassword)
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = oldPassword!,
            NewPassword = "newpassword123"
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OldPassword" && e.ErrorMessage == "Current password is required");
    }

    [Fact]
    public void Validate_WithOldPasswordTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longPassword = new string('a', 101); // 101 characters
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = longPassword,
            NewPassword = "newpassword123"
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OldPassword" && e.ErrorMessage == "Password cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidNewPassword_ShouldHaveValidationError(string? newPassword)
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = "oldpassword123",
            NewPassword = newPassword!
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword" && e.ErrorMessage == "New password is required");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("12345")]
    public void Validate_WithNewPasswordTooShort_ShouldHaveValidationError(string newPassword)
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = "oldpassword123",
            NewPassword = newPassword!
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword" && e.ErrorMessage == "New password must be at least 6 characters long");
    }

    [Fact]
    public void Validate_WithNewPasswordTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longPassword = new string('a', 101); // 101 characters
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = "oldpassword123",
            NewPassword = longPassword
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword" && e.ErrorMessage == "New password cannot exceed 100 characters");
    }

    [Fact]
    public void Validate_WithNewPasswordSameAsOldPassword_ShouldHaveValidationError()
    {
        // Arrange
        var samePassword = "samepassword123";
        var changePasswordDto = new ChangePasswordDto
        {
            OldPassword = samePassword,
            NewPassword = samePassword
        };

        // Act
        var result = _validator.Validate(changePasswordDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword" && e.ErrorMessage == "New password must be different from the current password");
    }

}