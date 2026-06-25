using FluentValidation;
using ProjectManagement.Application.DTOs.Auth;

namespace ProjectManagement.Application.Validators.Auth;

/// <summary>
/// Validator for RefreshTokenRequest.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
