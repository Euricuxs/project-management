using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Project;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for project operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects for a workspace.
    /// </summary>
    [HttpGet("workspace/{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkspaceProjects(
        Guid workspaceId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var projects = await _projectService.GetWorkspaceProjectsAsync(workspaceId, userId.Value, includeArchived, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProjectListItemResponse>>.SuccessResponse(projects));
    }

    /// <summary>
    /// Get a project by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var project = await _projectService.GetProjectByIdAsync(id, userId.Value, cancellationToken);
        if (project == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Project not found or you don't have access",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Project not found or you don't have access" } }
            ));
        }

        return Ok(ApiResponse<ProjectResponse>.SuccessResponse(project));
    }

    /// <summary>
    /// Create a new project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var project = await _projectService.CreateProjectAsync(request, userId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id },
            ApiResponse<ProjectResponse>.SuccessResponse(project, "Project created successfully"));
    }

    /// <summary>
    /// Update an existing project.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var project = await _projectService.UpdateProjectAsync(id, request, userId.Value, cancellationToken);
        return Ok(ApiResponse<ProjectResponse>.SuccessResponse(project, "Project updated successfully"));
    }

    /// <summary>
    /// Archive a project.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveProject(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var success = await _projectService.ArchiveProjectAsync(id, userId.Value, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Project not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Project not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { archived = true }, "Project archived successfully"));
    }

    /// <summary>
    /// Restore an archived project.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreProject(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var success = await _projectService.RestoreProjectAsync(id, userId.Value, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Project not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Project not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { restored = true }, "Project restored successfully"));
    }

    /// <summary>
    /// Delete a project.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var success = await _projectService.DeleteProjectAsync(id, userId.Value, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Project not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Project not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }, "Project deleted successfully"));
    }
}
