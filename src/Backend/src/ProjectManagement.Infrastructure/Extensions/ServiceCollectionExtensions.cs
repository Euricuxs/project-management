using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Infrastructure.Services;

namespace ProjectManagement.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services)
    {
        // Authentication services
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Workspace services
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        // Project services
        services.AddScoped<IProjectService, ProjectService>();

        // Dashboard services
        services.AddScoped<IDashboardService, DashboardService>();

        // Board services
        services.AddScoped<IBoardService, BoardService>();

        // Task services
        services.AddScoped<ITaskService, TaskService>();

        // Label services
        services.AddScoped<ILabelService, LabelService>();

        // Activity services
        services.AddScoped<IActivityService, ActivityService>();

        return services;
    }
}
