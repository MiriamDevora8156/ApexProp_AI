using FluentValidation;
using ApexProp.Application.DTOs;

namespace ApexProp.Application.Validators;

/// <summary>
/// CreateUserValidator - בדיקות לרישום משתמש
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        // Full Name
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .Length(3, 200).WithMessage("Full name must be between 3 and 200 characters");

        // Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid")
            .Length(5, 200).WithMessage("Email must be between 5 and 200 characters");

        // Password
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Length(8, 100).WithMessage("Password must be between 8 and 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character (!@#$%^&*)");

        // Confirm Password
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");
    }
}