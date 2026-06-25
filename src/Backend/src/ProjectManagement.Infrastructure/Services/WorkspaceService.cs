using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Workspace;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for workspace operations.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly ApplicationDbContext _context;

    public WorkspaceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkspaceListItemResponse>> GetUserWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get workspace IDs the user is a member of
        var workspaceIds = await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId && !wm.IsDeleted)
            .Select(wm => new { wm.WorkspaceId, wm.Role })
            .ToListAsync(cancellationToken);

        if (workspaceIds.Count == 0)
            return new List<WorkspaceListItemResponse>();

        var idsWithRoles = workspaceIds.ToDictionary(x => x.WorkspaceId, x => x.Role);

        // Get workspace details with counts in a single optimized query
        var workspaces = await _context.Workspaces
            .AsNoTracking()
            .Where(w => idsWithRoles.Keys.Contains(w.Id) && !w.IsDeleted)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.LogoUrl,
                w.IsPublic,
                w.CreatedAt,
                MemberCount = _context.WorkspaceMembers.Count(m => m.WorkspaceId == w.Id && !m.IsDeleted),
                ProjectCount = _context.Projects.Count(p => p.WorkspaceId == w.Id && !p.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        return workspaces
            .Select(w => new WorkspaceListItemResponse
            {
                Id = w.Id,
                Name = w.Name,
                LogoUrl = w.LogoUrl,
                IsPublic = w.IsPublic,
                Role = idsWithRoles[w.Id].ToString(),
                MemberCount = w.MemberCount,
                ProjectCount = w.ProjectCount,
                CreatedAt = w.CreatedAt
            })
            .ToList();
    }

    public async Task<WorkspaceResponse?> GetWorkspaceByIdAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .Include(w => w.Projects)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted, cancellationToken);

        if (workspace == null)
            return null;

        // Check if user has access (is member or workspace is public)
        var isMember = workspace.Members.Any(m => m.UserId == userId);
        if (!workspace.IsPublic && !isMember)
            return null;

        return MapToResponse(workspace);
    }

    public async Task<WorkspaceResponse> CreateWorkspaceAsync(
        CreateWorkspaceRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate name for this user
        var existingWorkspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w =>
                w.Members.Any(m => m.UserId == userId) &&
                w.Name.ToLower() == request.Name.ToLower() &&
                !w.IsDeleted,
                cancellationToken);

        if (existingWorkspace != null)
            throw new AlreadyExistsException($"A workspace with the name '{request.Name}' already exists.");

        var workspace = new Workspace
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            LogoUrl = request.LogoUrl,
            OwnerId = userId,
            IsPublic = request.IsPublic
        };

        _context.Workspaces.Add(workspace);

        // Add owner as a member with Owner role
        var membership = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = Domain.Enums.WorkspaceRole.Owner
        };

        _context.WorkspaceMembers.Add(membership);

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .Include(w => w.Projects)
            .FirstAsync(w => w.Id == workspace.Id, cancellationToken);

        return MapToResponse(workspace);
    }

    public async Task<WorkspaceResponse> UpdateWorkspaceAsync(
        Guid workspaceId,
        UpdateWorkspaceRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .Include(w => w.Projects)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted, cancellationToken);

        if (workspace == null)
            throw new NotFoundException("Workspace not found.");

        // Check if user is owner or admin
        var membership = workspace.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || (membership.Role != Domain.Enums.WorkspaceRole.Owner && membership.Role != Domain.Enums.WorkspaceRole.Admin))
            throw new ForbiddenException("You do not have permission to update this workspace.");

        // Check for duplicate name (excluding current workspace) - optimized query
        var duplicateName = await _context.WorkspaceMembers
            .AnyAsync(wm =>
                wm.UserId == userId &&
                !wm.IsDeleted &&
                !wm.Workspace!.IsDeleted &&
                wm.WorkspaceId != workspaceId &&
                wm.Workspace!.Name.ToLower() == request.Name.ToLower(),
                cancellationToken);

        if (duplicateName)
            throw new AlreadyExistsException($"A workspace with the name '{request.Name}' already exists.");

        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description?.Trim();
        workspace.LogoUrl = request.LogoUrl;
        workspace.IsPublic = request.IsPublic;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(workspace);
    }

    public async Task<bool> DeleteWorkspaceAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted, cancellationToken);

        if (workspace == null)
            return false;

        // Only owner can delete
        var membership = workspace.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || membership.Role != Domain.Enums.WorkspaceRole.Owner)
            throw new ForbiddenException("Only the workspace owner can delete this workspace.");

        // Check if this is the user's last workspace
        var userWorkspaceCount = await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId && !wm.Workspace!.IsDeleted)
            .CountAsync(cancellationToken);

        if (userWorkspaceCount <= 1)
            throw new BusinessRuleException("Cannot delete your last workspace. You must have at least one workspace.");

        // Soft delete
        workspace.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UserHasAccessAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted, cancellationToken);

        if (workspace == null)
            return false;

        // Public workspaces are accessible to all
        if (workspace.IsPublic)
            return true;

        // Check if user is a member
        return workspace.Members.Any(m => m.UserId == userId);
    }

    public async Task<int> GetUserWorkspaceCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId && !wm.Workspace!.IsDeleted)
            .CountAsync(cancellationToken);
    }

    private static WorkspaceResponse MapToResponse(Workspace workspace)
    {
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            LogoUrl = workspace.LogoUrl,
            OwnerId = workspace.OwnerId,
            OwnerName = workspace.Owner?.FullName ?? string.Empty,
            IsPublic = workspace.IsPublic,
            MemberCount = workspace.Members.Count,
            ProjectCount = workspace.Projects.Count(p => !p.IsDeleted),
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt
        };
    }
}