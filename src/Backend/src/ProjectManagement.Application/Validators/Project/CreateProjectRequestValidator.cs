using FluentValidation;
using ProjectManagement.Application.DTOs.Project;

namespace ProjectManagement.Application.Validators.Project;

/// <summary>
/// Validator for CreateProjectRequest.
/// </summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MinimumLength(3).WithMessage("Project name must be at least 3 characters.")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters.");

        RuleFor(x => x.Key)
            .MaximumLength(20).WithMessage("Project key must not exceed 20 characters.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Project key can only contain uppercase letters, numbers, and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Key));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color code.")
            .When(x => !string.IsNullOrEmpty(x.Color));

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
