using FluentValidation;
using ApexProp.Application.DTOs;

namespace ApexProp.Application.Validators;

/// <summary>
/// CreatePropertyValidator - בדיקות לפני שמאחסנים בDB
/// זה יותר מדויק מ-DataAnnotations
/// </summary>
public class CreatePropertyValidator : AbstractValidator<CreatePropertyDto>
{
    public CreatePropertyValidator()
    {
        // Title
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\u0590-\u05FF\s\-,'""]+$").WithMessage("Title contains invalid characters");

        // Address
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .Length(5, 500).WithMessage("Address must be between 5 and 500 characters");

        // Latitude
        RuleFor(x => x.Latitude)
            .NotEmpty().WithMessage("Latitude is required")
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .Must(lat => lat != 0).WithMessage("Latitude cannot be 0");

        // Longitude
        RuleFor(x => x.Longitude)
            .NotEmpty().WithMessage("Longitude is required")
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .Must(lng => lng != 0).WithMessage("Longitude cannot be 0");

        // Price
        RuleFor(x => x.Price)
            .NotEmpty().WithMessage("Price is required")
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(999999999.99m).WithMessage("Price cannot exceed 999,999,999.99");

        // Rooms
        RuleFor(x => x.Rooms)
            .NotEmpty().WithMessage("Rooms is required")
            .GreaterThanOrEqualTo(1).WithMessage("Rooms must be at least 1")
            .LessThanOrEqualTo(20).WithMessage("Rooms cannot exceed 20");

        // AreaSqm
        RuleFor(x => x.AreaSqm)
            .NotEmpty().WithMessage("Area is required")
            .GreaterThan(0.1).WithMessage("Area must be greater than 0.1 sqm")
            .LessThanOrEqualTo(10000).WithMessage("Area cannot exceed 10,000 sqm");
    }
}