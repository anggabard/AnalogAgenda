using Database.DTOs;
using FluentValidation;

namespace AnalogAgenda.Server.Validators;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty()
            .WithMessage("Current password is required")
            .MaximumLength(100)
            .WithMessage("Password cannot exceed 100 characters");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(6)
            .WithMessage("New password must be at least 6 characters long")
            .MaximumLength(100)
            .WithMessage("New password cannot exceed 100 characters");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.OldPassword)
            .WithMessage("New password must be different from the current password")
            .When(x => !string.IsNullOrEmpty(x.OldPassword) && !string.IsNullOrEmpty(x.NewPassword));
    }
}
