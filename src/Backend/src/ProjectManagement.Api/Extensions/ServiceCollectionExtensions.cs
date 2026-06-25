using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Filters;
using ProjectManagement.Api.Middleware;
using ProjectManagement.Infrastructure.Data;
using ProjectManagement.Infrastructure.Extensions;

namespace ProjectManagement.Api.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API layer services to the service collection.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApplicationExceptionFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Add OpenAPI for API documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Project Management API",
                Version = "v1",
                Description = "A production-ready Project Management SaaS API"
            });
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds Application layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add FluentValidation
        services.AddFluentValidationServices();

        return services;
    }

    /// <summary>
    /// Adds Infrastructure layer services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(true);
        });

        // Add HttpContextAccessor for accessing HTTP context in services
        services.AddHttpContextAccessor();

        // Add Repositories and Services
        services.AddInfrastructureDependencies();

        return services;
    }

    /// <summary>
    /// Configures the application pipeline.
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Global exception handling (must be first)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Request logging
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Project Management API v1");
            options.RoutePrefix = "swagger";
        });

        // CORS
        app.UseCors("AllowFrontend");

        // HTTPS redirection (uncomment in production)
        // app.UseHttpsRedirection();

        // Routing
        app.UseRouting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        return app;
    }
}
