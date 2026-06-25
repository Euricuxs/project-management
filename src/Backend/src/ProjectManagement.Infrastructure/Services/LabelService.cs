using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Label;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for label operations.
/// </summary>
public class LabelService : ILabelService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;

    public LabelService(ApplicationDbContext context, IProjectService projectService)
    {
        _context = context;
        _projectService = projectService;
    }

    public async Task<IReadOnlyList<LabelListItemResponse>> GetProjectLabelsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var labels = await _context.Labels
            .Where(l => l.ProjectId == projectId && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

        // Get task counts separately to avoid query filter issues
        var labelIds = labels.Select(l => l.Id).ToList();
        var taskCounts = await _context.TaskLabels
            .Where(tl => labelIds.Contains(tl.LabelId))
            .GroupBy(tl => tl.LabelId)
            .Select(g => new { LabelId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LabelId, x => x.Count, cancellationToken);

        return labels.Select(l => new LabelListItemResponse
        {
            Id = l.Id,
            ProjectId = l.ProjectId,
            Name = l.Name,
            Color = l.Color,
            TaskCount = taskCounts.TryGetValue(l.Id, out var count) ? count : 0
        }).ToList();
    }

    public async Task<LabelResponse?> GetLabelByIdAsync(
        Guid labelId,
        CancellationToken cancellationToken = default)
    {
        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && !l.IsDeleted, cancellationToken);

        if (label == null)
            return null;

        var taskCount = await _context.TaskLabels
            .CountAsync(tl => tl.LabelId == labelId, cancellationToken);

        return new LabelResponse
        {
            Id = label.Id,
            ProjectId = label.ProjectId,
            Name = label.Name,
            Color = label.Color,
            TaskCount = taskCount,
            CreatedAt = label.CreatedAt,
            UpdatedAt = label.UpdatedAt
        };
    }

    public async Task<LabelResponse> CreateLabelAsync(
        Guid projectId,
        CreateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check project exists
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            throw new NotFoundException("Project not found.");

        // Check for duplicate label name (case-insensitive) in the same project
        var existingLabel = await _context.Labels
            .AnyAsync(l => l.ProjectId == projectId &&
                          l.Name.ToLower() == request.Name.ToLower() &&
                          !l.IsDeleted, cancellationToken);

        if (existingLabel)
            throw new AlreadyExistsException($"A label with the name '{request.Name}' already exists in this project.");

        var label = new Label
        {
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Color = request.Color.ToUpper()
        };

        _context.Labels.Add(label);
        await _context.SaveChangesAsync(cancellationToken);

        return new LabelResponse
        {
            Id = label.Id,
            ProjectId = label.ProjectId,
            Name = label.Name,
            Color = label.Color,
            TaskCount = 0,
            CreatedAt = label.CreatedAt,
            UpdatedAt = label.UpdatedAt
        };
    }

    public async Task<LabelResponse> UpdateLabelAsync(
        Guid labelId,
        UpdateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && !l.IsDeleted, cancellationToken);

        if (label == null)
            throw new NotFoundException("Label not found.");

        // Check for duplicate name if name is being updated (case-insensitive)
        if (!string.IsNullOrEmpty(request.Name))
        {
            var existingLabel = await _context.Labels
                .AnyAsync(l => l.ProjectId == label.ProjectId &&
                              l.Name.ToLower() == request.Name.ToLower() &&
                              l.Id != labelId &&
                              !l.IsDeleted, cancellationToken);

            if (existingLabel)
                throw new AlreadyExistsException($"A label with the name '{request.Name}' already exists in this project.");
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Name))
            label.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.Color))
            label.Color = request.Color.ToUpper();

        await _context.SaveChangesAsync(cancellationToken);

        var taskCount = await _context.TaskLabels
            .CountAsync(tl => tl.LabelId == labelId, cancellationToken);

        return new LabelResponse
        {
            Id = label.Id,
            ProjectId = label.ProjectId,
            Name = label.Name,
            Color = label.Color,
            TaskCount = taskCount,
            CreatedAt = label.CreatedAt,
            UpdatedAt = label.UpdatedAt
        };
    }

    public async Task<bool> DeleteLabelAsync(
        Guid labelId,
        CancellationToken cancellationToken = default)
    {
        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && !l.IsDeleted, cancellationToken);

        if (label == null)
            return false;

        // Remove all task-label associations
        var taskLabels = await _context.TaskLabels
            .Where(tl => tl.LabelId == labelId)
            .ToListAsync(cancellationToken);
        _context.TaskLabels.RemoveRange(taskLabels);

        // Soft delete the label
        label.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AddLabelsToTaskAsync(
        Guid taskId,
        AddLabelsToTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.TaskLabels)
            .Include(t => t.Column)
                .ThenInclude(c => c!.Board)
                    .ThenInclude(b => b!.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);

        if (task == null)
            throw new NotFoundException("Task not found.");

        var projectId = task.Column?.Board?.Project?.Id;
        if (projectId == null)
            throw new NotFoundException("Project not found.");

        var validLabelIds = await _context.Labels
            .Where(l => l.ProjectId == projectId.Value && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        // Add only valid labels that aren't already assigned
        foreach (var labelId in request.LabelIds)
        {
            if (validLabelIds.Contains(labelId) &&
                !task.TaskLabels.Any(tl => tl.LabelId == labelId))
            {
                task.TaskLabels.Add(new TaskLabel
                {
                    TaskId = taskId,
                    LabelId = labelId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveLabelFromTaskAsync(
        Guid taskId,
        Guid labelId,
        CancellationToken cancellationToken = default)
    {
        var taskLabel = await _context.TaskLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId, cancellationToken);

        if (taskLabel == null)
            return false;

        _context.TaskLabels.Remove(taskLabel);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<LabelListItemResponse>> GetTaskLabelsAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var taskLabels = await _context.TaskLabels
            .Where(tl => tl.TaskId == taskId)
            .ToListAsync(cancellationToken);

        var labelIds = taskLabels.Select(tl => tl.LabelId).ToList();
        var labels = await _context.Labels
            .Where(l => labelIds.Contains(l.Id) && !l.IsDeleted)
            .ToListAsync(cancellationToken);

        var taskCounts = await _context.TaskLabels
            .Where(tl => labelIds.Contains(tl.LabelId))
            .GroupBy(tl => tl.LabelId)
            .Select(g => new { LabelId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LabelId, x => x.Count, cancellationToken);

        return labels.Select(l => new LabelListItemResponse
        {
            Id = l.Id,
            ProjectId = l.ProjectId,
            Name = l.Name,
            Color = l.Color,
            TaskCount = taskCounts.TryGetValue(l.Id, out var count) ? count : 0
        }).ToList();
    }
}
