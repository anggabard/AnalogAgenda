using AnalogAgenda.Server.Validators;
using Database.DBObjects.Enums;
using Database.DTOs;

namespace AnalogAgenda.Server.Tests.Validators;

public class FilmDtoValidatorTests
{
    private readonly FilmDtoValidator _validator;

    public FilmDtoValidatorTests()
    {
        _validator = new FilmDtoValidator();
    }

    [Theory]
    [InlineData("100")]
    [InlineData("200")]
    [InlineData("400")]
    [InlineData("800")]
    [InlineData("1600")]
    [InlineData("3200")]
    [InlineData("6400")]
    public void Validate_WithValidSingleIso_ShouldNotHaveValidationErrors(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("100-400")]
    [InlineData("200-800")]
    [InlineData("50-200")]
    [InlineData("400-1600")]
    [InlineData("100-3200")]
    public void Validate_WithValidIsoRange_ShouldNotHaveValidationErrors(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyIso_ShouldHaveValidationError(string? iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso!);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Iso" && e.ErrorMessage == "ISO is required"
        );
    }

    [Theory]
    [InlineData("100 - 400")]
    [InlineData("100 -400")]
    [InlineData("100- 400")]
    [InlineData("200 800")]
    [InlineData(" 400")]
    [InlineData("400 ")]
    public void Validate_WithIsoContainingSpaces_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("-400")]
    public void Validate_WithIsoZeroOrNegative_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("400-100")] // First greater than second
    [InlineData("800-200")] // First greater than second
    [InlineData("400-400")] // First equal to second
    public void Validate_WithInvalidIsoRange_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("0-400")] // First is zero
    [InlineData("100-0")] // Second is zero
    [InlineData("-100-400")] // First is negative
    [InlineData("100--400")] // Second is negative
    public void Validate_WithIsoRangeContainingZeroOrNegative_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("ISO400")]
    [InlineData("400ISO")]
    [InlineData("one hundred")]
    public void Validate_WithNonNumericIso_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("100/400")] // Slash instead of dash
    [InlineData("100to400")] // Word "to" instead of dash
    [InlineData("100..400")] // Double dot instead of dash
    [InlineData("100_400")] // Underscore instead of dash
    public void Validate_WithInvalidSeparator_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("100-200-400")] // Three parts
    [InlineData("100-200-400-800")] // Four parts
    public void Validate_WithMultipleDashes_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    [Theory]
    [InlineData("100-abc")]
    [InlineData("abc-400")]
    public void Validate_WithNonNumericRangeParts_ShouldHaveValidationError(string iso)
    {
        // Arrange
        var filmDto = CreateValidFilmDto(iso);

        // Act
        var result = _validator.Validate(filmDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Iso");
    }

    private FilmDto CreateValidFilmDto(string iso)
    {
        return new FilmDto
        {
            RowKey = "",
            Name = "Test Film",
            Iso = iso,
            Type = EFilmType.ColorNegative.ToString(),
            NumberOfExposures = 36,
            Cost = 10.0,
            PurchasedBy = EUsernameType.Angel.ToString(),
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            ImageBase64 = "",
            Description = "Test description",
            Developed = false,
        };
    }
}
