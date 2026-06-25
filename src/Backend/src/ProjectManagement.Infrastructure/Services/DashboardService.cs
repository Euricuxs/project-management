using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Dashboard;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for dashboard operations.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponse> GetDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var endOfWeek = today.AddDays(7);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // Get user's workspace IDs
        var workspaceIds = await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId)
            .Select(wm => wm.WorkspaceId)
            .ToListAsync(cancellationToken);

        if (workspaceIds.Count == 0)
        {
            return new DashboardResponse
            {
                ActiveProjects = 0,
                CompletedProjects = 0,
                TasksDueThisWeek = 0,
                CompletedTasksThisMonth = 0,
                TeamMembers = 0,
                RecentActivities = new List<RecentActivityItem>(),
                UpcomingTasks = new List<UpcomingTaskItem>()
            };
        }

        // Get all project IDs for user's workspaces
        var projectIds = await _context.Projects
            .Where(p => workspaceIds.Contains(p.WorkspaceId) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // Get all board IDs for those projects
        var boardIds = await _context.Boards
            .Where(b => projectIds.Contains(b.ProjectId) && !b.IsDeleted)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        // Get all column IDs for those boards
        var columnIds = await _context.Columns
            .Where(c => boardIds.Contains(c.BoardId) && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        // Use database-side aggregations instead of loading all data into memory
        var tasksQuery = _context.Tasks
            .Where(t => columnIds.Contains(t.ColumnId) && !t.IsDeleted);

        // Count active projects
        var activeProjects = await _context.Projects
            .Where(p => projectIds.Contains(p.Id) && p.Status == ProjectStatus.Active && !p.IsDeleted)
            .CountAsync(cancellationToken);

        // Count completed projects
        var completedProjects = await _context.Projects
            .Where(p => projectIds.Contains(p.Id) && p.Status == ProjectStatus.Completed && !p.IsDeleted)
            .CountAsync(cancellationToken);

        // Count tasks due this week (database-side)
        var tasksDueThisWeek = await tasksQuery
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate.Value.Date >= today &&
                        t.DueDate.Value.Date <= endOfWeek &&
                        t.Status != Domain.Enums.TaskStatus.Done &&
                        t.Status != Domain.Enums.TaskStatus.Cancelled)
            .CountAsync(cancellationToken);

        // Count completed tasks this month (database-side)
        var completedTasksThisMonth = await tasksQuery
            .Where(t => t.CompletedAt.HasValue &&
                        t.CompletedAt.Value.Date >= startOfMonth &&
                        t.CompletedAt.Value.Date <= endOfMonth)
            .CountAsync(cancellationToken);

        // Count team members (database-side)
        var teamMemberIds = await _context.WorkspaceMembers
            .Where(wm => workspaceIds.Contains(wm.WorkspaceId))
            .Select(wm => wm.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Get upcoming tasks (database-side with projection)
        var upcomingTasks = await tasksQuery
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate.Value.Date >= today &&
                        t.DueDate.Value.Date <= endOfWeek &&
                        t.Status != Domain.Enums.TaskStatus.Done &&
                        t.Status != Domain.Enums.TaskStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Select(t => new UpcomingTaskItem
            {
                Id = t.Id,
                Title = t.Title,
                DueDate = t.DueDate!.Value,
                Status = t.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        // Build project map for upcoming tasks
        if (upcomingTasks.Count > 0)
        {
            var taskIds = upcomingTasks.Select(t => t.Id).ToList();
            var taskColumns = await _context.Tasks
                .Where(t => taskIds.Contains(t.Id))
                .Select(t => new { t.Id, t.ColumnId })
                .ToListAsync(cancellationToken);

            var columnBoardIds = await _context.Columns
                .Where(c => taskColumns.Select(tc => tc.ColumnId).Contains(c.Id))
                .Select(c => new { c.Id, c.BoardId })
                .ToListAsync(cancellationToken);

            var boardProjectIds = await _context.Boards
                .Where(b => columnBoardIds.Select(cb => cb.BoardId).Contains(b.Id))
                .Select(b => new { b.Id, b.ProjectId })
                .ToListAsync(cancellationToken);

            var projectNames = await _context.Projects
                .Where(p => boardProjectIds.Select(bp => bp.ProjectId).Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);

            var projectNameMap = projectNames.ToDictionary(p => p.Id, p => p.Name);
            var boardProjectMap = boardProjectIds.ToDictionary(b => b.Id, b => b.ProjectId);
            var columnBoardMap = columnBoardIds.ToDictionary(c => c.Id, c => c.BoardId);
            var taskColumnMap = taskColumns.ToDictionary(t => t.Id, t => t.ColumnId);

            foreach (var task in upcomingTasks)
            {
                if (taskColumnMap.TryGetValue(task.Id, out var columnId) &&
                    columnBoardMap.TryGetValue(columnId, out var boardId) &&
                    boardProjectMap.TryGetValue(boardId, out var projectId))
                {
                    task.ProjectId = projectId;
                    task.ProjectName = projectNameMap.GetValueOrDefault(projectId);
                }
            }
        }

        // Build recent activities
        var recentActivities = new List<RecentActivityItem>();

        // Projects created recently
        var recentProjects = await _context.Projects
            .Where(p => projectIds.Contains(p.Id) && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new { p.Id, p.Name, p.CreatedAt })
            .ToListAsync(cancellationToken);

        foreach (var project in recentProjects)
        {
            recentActivities.Add(new RecentActivityItem
            {
                Id = project.Id,
                Type = "project_created",
                Description = $"Created project '{project.Name}'",
                ProjectName = project.Name,
                ProjectId = project.Id,
                CreatedAt = project.CreatedAt
            });
        }

        // Tasks completed recently
        var recentCompletedTasks = await tasksQuery
            .Where(t => t.CompletedAt.HasValue)
            .OrderByDescending(t => t.CompletedAt)
            .Take(5)
            .Select(t => new { t.Id, t.Title, t.CompletedAt, t.ColumnId })
            .ToListAsync(cancellationToken);

        if (recentCompletedTasks.Count > 0)
        {
            var completedTaskColumnIds = recentCompletedTasks.Select(t => t.ColumnId).ToList();
            var completedTaskColumns = await _context.Columns
                .Where(c => completedTaskColumnIds.Contains(c.Id))
                .Select(c => new { c.Id, c.BoardId })
                .ToListAsync(cancellationToken);

            var completedTaskBoards = await _context.Boards
                .Where(b => completedTaskColumns.Select(c => c.BoardId).Contains(b.Id))
                .Select(b => new { b.Id, b.ProjectId })
                .ToListAsync(cancellationToken);

            var completedTaskProjectIds = completedTaskBoards.Select(b => b.ProjectId).Distinct().ToList();
            var completedTaskProjects = await _context.Projects
                .Where(p => completedTaskProjectIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);

            var completedProjectMap = completedTaskProjects.ToDictionary(p => p.Id, p => p.Name);
            var completedBoardProjectMap = completedTaskBoards.ToDictionary(b => b.Id, b => b.ProjectId);
            var completedColumnBoardMap = completedTaskColumns.ToDictionary(c => c.Id, c => c.BoardId);

            foreach (var task in recentCompletedTasks)
            {
                Guid? projectId = null;
                string? projectName = null;

                if (completedColumnBoardMap.TryGetValue(task.ColumnId, out var cbId) &&
                    completedBoardProjectMap.TryGetValue(cbId, out var bpId))
                {
                    projectId = bpId;
                    projectName = completedProjectMap.GetValueOrDefault(bpId);
                }

                recentActivities.Add(new RecentActivityItem
                {
                    Id = task.Id,
                    Type = "task_completed",
                    Description = $"Completed task '{task.Title}'",
                    ProjectName = projectName,
                    ProjectId = projectId,
                    CreatedAt = task.CompletedAt ?? DateTime.UtcNow
                });
            }
        }

        // Sort by created date descending and take top 10
        var sortedActivities = recentActivities
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();

        return new DashboardResponse
        {
            ActiveProjects = activeProjects,
            CompletedProjects = completedProjects,
            TasksDueThisWeek = tasksDueThisWeek,
            CompletedTasksThisMonth = completedTasksThisMonth,
            TeamMembers = teamMemberIds,
            RecentActivities = sortedActivities,
            UpcomingTasks = upcomingTasks
        };
    }
}
