using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Workspace;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for workspace operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class WorkspacesController : BaseApiController
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<WorkspacesController> _logger;

    public WorkspacesController(IWorkspaceService workspaceService, ILogger<WorkspacesController> logger)
    {
        _workspaceService = workspaceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all workspaces for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's workspaces</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkspaceListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserWorkspaces(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkspaceListItemResponse>>.SuccessResponse(workspaces));
    }

    /// <summary>
    /// Get a workspace by ID.
    /// </summary>
    /// <param name="id">Workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workspace details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var workspace = await _workspaceService.GetWorkspaceByIdAsync(id, userId.Value, cancellationToken);
        if (workspace == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Workspace not found or you don't have access",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Workspace not found or you don't have access" } }
            ));
        }

        return Ok(ApiResponse<WorkspaceResponse>.SuccessResponse(workspace));
    }

    /// <summary>
    /// Create a new workspace.
    /// </summary>
    /// <param name="request">Workspace creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created workspace</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var workspace = await _workspaceService.CreateWorkspaceAsync(request, userId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id },
            ApiResponse<WorkspaceResponse>.SuccessResponse(workspace, "Workspace created successfully"));
    }

    /// <summary>
    /// Update an existing workspace.
    /// </summary>
    /// <param name="id">Workspace ID</param>
    /// <param name="request">Updated workspace details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated workspace</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var workspace = await _workspaceService.UpdateWorkspaceAsync(id, request, userId.Value, cancellationToken);
        return Ok(ApiResponse<WorkspaceResponse>.SuccessResponse(workspace, "Workspace updated successfully"));
    }

    /// <summary>
    /// Delete a workspace (soft delete).
    /// </summary>
    /// <param name="id">Workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var success = await _workspaceService.DeleteWorkspaceAsync(id, userId.Value, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Workspace not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Workspace not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }, "Workspace deleted successfully"));
    }
}
