using FluentValidation;
using ProjectManagement.Application.DTOs.Workspace;

namespace ProjectManagement.Application.Validators.Workspace;

/// <summary>
/// Validator for UpdateWorkspaceRequest.
/// </summary>
public class UpdateWorkspaceRequestValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MinimumLength(3).WithMessage("Workspace name must be at least 3 characters.")
            .MaximumLength(200).WithMessage("Workspace name must not exceed 200 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Workspace name can only contain letters, numbers, spaces, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.LogoUrl)
            .MaximumLength(1024).WithMessage("Logo URL must not exceed 1024 characters.")
            .Must(BeAValidUrl).WithMessage("Please provide a valid URL for the logo.")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
