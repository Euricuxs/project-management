using FluentValidation;
using ProjectManagement.Application.DTOs.Label;

namespace ProjectManagement.Application.Validators.Label;

/// <summary>
/// Validator for UpdateLabelRequest.
/// </summary>
public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(1).WithMessage("Label name must be at least 1 character.")
            .MaximumLength(100).WithMessage("Label name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g., #FF5733).")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}
