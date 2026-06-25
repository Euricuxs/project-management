using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Activity;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for activity/audit log operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly IProjectService _projectService;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        IActivityService activityService,
        IProjectService projectService,
        ILogger<ActivitiesController> logger)
    {
        _activityService = activityService;
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get activities for a project.
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ActivityListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProjectActivities(
        Guid projectId,
        [FromQuery] ActivityQueryParams query,
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

        if (!await _projectService.UserHasAccessAsync(projectId, userId.Value, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.ErrorResponse(
                "You do not have access to this project",
                new List<ApiError> { new() { Code = "FORBIDDEN", Message = "You do not have access to this project" } }
            ));
        }

        query.ProjectId = projectId;
        var activities = await _activityService.GetProjectActivitiesAsync(projectId, query, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ActivityListItemResponse>>.SuccessResponse(activities));
    }

    /// <summary>
    /// Get activities for a specific entity.
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActivityListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEntityActivities(
        string entityType,
        Guid entityId,
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

        var activities = await _activityService.GetEntityActivitiesAsync(entityType, entityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ActivityListItemResponse>>.SuccessResponse(activities));
    }

    /// <summary>
    /// Get recent activities for the current user.
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActivityListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecentActivities(
        [FromQuery] int limit = 10,
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

        var activities = await _activityService.GetRecentActivitiesAsync(userId.Value, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ActivityListItemResponse>>.SuccessResponse(activities));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
