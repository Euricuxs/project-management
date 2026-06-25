using ProjectManagement.Application.DTOs.Dashboard;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for dashboard operations.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics for a user.
    /// </summary>
    Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
}
