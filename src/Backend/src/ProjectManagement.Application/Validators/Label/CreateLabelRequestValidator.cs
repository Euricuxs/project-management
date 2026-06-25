using FluentValidation;
using ProjectManagement.Application.DTOs.Label;

namespace ProjectManagement.Application.Validators.Label;

/// <summary>
/// Validator for CreateLabelRequest.
/// </summary>
public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required.")
            .MinimumLength(1).WithMessage("Label name must be at least 1 character.")
            .MaximumLength(100).WithMessage("Label name must not exceed 100 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g., #FF5733).");
    }
}
