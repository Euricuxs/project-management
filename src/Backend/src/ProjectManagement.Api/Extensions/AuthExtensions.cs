using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.Infrastructure.Services;

namespace ProjectManagement.Api.Extensions;

/// <summary>
/// Extension methods for configuring authentication and authorization.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Adds JWT authentication services.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "ProjectManagement";
        var jwtAudience = configuration["Jwt:Audience"] ?? "ProjectManagementApp";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var response = """{"success":false,"data":null,"message":"Authentication required","errors":[{"code":"UNAUTHORIZED","message":"Please login to access this resource"}]}""";
                    return context.Response.WriteAsync(response);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var response = """{"success":false,"data":null,"message":"Access denied","errors":[{"code":"FORBIDDEN","message":"You do not have permission to access this resource"}]}""";
                    return context.Response.WriteAsync(response);
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization policies.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy - requires authenticated user
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Policy for admin-only endpoints
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            // Policy for workspace owners
            options.AddPolicy("WorkspaceOwner", policy =>
                policy.RequireRole("Owner", "Admin"));

            // Policy for workspace admins
            options.AddPolicy("WorkspaceAdmin", policy =>
                policy.RequireRole("Owner", "Admin", "Member"));
        });

        return services;
    }
}
