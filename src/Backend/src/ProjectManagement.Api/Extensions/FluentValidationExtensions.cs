using FluentValidation;

namespace ProjectManagement.Api.Extensions;

/// <summary>
/// Extension methods for configuring FluentValidation.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Adds FluentValidation services to the service collection.
    /// </summary>
    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IValidatorMarker>();

        return services;
    }
}

/// <summary>
/// Marker interface for locating the Application assembly in FluentValidation.
/// </summary>
public interface IValidatorMarker
{
}
