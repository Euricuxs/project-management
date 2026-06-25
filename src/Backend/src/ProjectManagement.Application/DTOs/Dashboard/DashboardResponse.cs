namespace ProjectManagement.Application.DTOs.Dashboard;

/// <summary>
/// Response model for dashboard statistics.
/// </summary>
public class DashboardResponse
{
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int TasksDueThisWeek { get; set; }
    public int CompletedTasksThisMonth { get; set; }
    public int TeamMembers { get; set; }
    public List<RecentActivityItem> RecentActivities { get; set; } = new();
    public List<UpcomingTaskItem> UpcomingTasks { get; set; } = new();
}

/// <summary>
/// Recent activity item for dashboard.
/// </summary>
public class RecentActivityItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Upcoming task item for dashboard.
/// </summary>
public class UpcomingTaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
