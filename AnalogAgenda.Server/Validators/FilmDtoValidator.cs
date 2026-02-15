using Database.DTOs;
using FluentValidation;

namespace AnalogAgenda.Server.Validators;

public class FilmDtoValidator : AbstractValidator<FilmDto>
{
    public FilmDtoValidator()
    {
        RuleFor(x => x.Brand)
            .NotEmpty()
            .WithMessage("Brand is required");

        RuleFor(x => x.Iso)
            .NotEmpty()
            .WithMessage("ISO is required")
            .Must(BeValidIso)
            .WithMessage("ISO must be either a number greater than 0 (e.g., '400') or a valid range (e.g., '100-400') where the first number is less than the second. No spaces allowed.");
    }

    private bool BeValidIso(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
            return false;

        // Check if it contains spaces (not allowed)
        if (iso.Contains(' '))
            return false;

        // Check if it's a range (contains dash)
        if (iso.Contains('-'))
        {
            var parts = iso.Split('-');
            
            // Must have exactly 2 parts
            if (parts.Length != 2)
                return false;

            // Both parts must be valid positive integers
            if (!int.TryParse(parts[0], out int first) || !int.TryParse(parts[1], out int second))
                return false;

            // Both must be greater than 0
            if (first <= 0 || second <= 0)
                return false;

            // First must be less than second
            if (first >= second)
                return false;

            return true;
        }
        else
        {
            // Single number case
            if (!int.TryParse(iso, out int value))
                return false;

            // Must be greater than 0
            return value > 0;
        }
    }
}

