using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectManagement.Infrastructure.Data;

/// <summary>
/// Provides extension methods for database initialization.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Applying database migrations...");

            // Apply all pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Found {Count} pending migrations to apply.", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found.");
                // EnsureCreated for initial database creation if no migrations exist
                await context.Database.EnsureCreatedAsync();
            }

            logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}