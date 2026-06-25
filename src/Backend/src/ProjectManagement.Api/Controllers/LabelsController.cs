using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Label;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for label operations.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/labels")]
[Authorize]
[Produces("application/json")]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IProjectService _projectService;
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(
        ILabelService labelService,
        IProjectService projectService,
        ILogger<LabelsController> logger)
    {
        _labelService = labelService;
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all labels for a project.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabelListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProjectLabels(
        Guid projectId,
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

        var labels = await _labelService.GetProjectLabelsAsync(projectId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LabelListItemResponse>>.SuccessResponse(labels));
    }

    /// <summary>
    /// Get a single label by ID.
    /// </summary>
    [HttpGet("{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabel(
        Guid projectId,
        Guid labelId,
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

        var label = await _labelService.GetLabelByIdAsync(labelId, cancellationToken);
        if (label == null || label.ProjectId != projectId)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Label not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Label not found" } }
            ));
        }

        return Ok(ApiResponse<LabelResponse>.SuccessResponse(label));
    }

    /// <summary>
    /// Create a new label.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLabel(
        Guid projectId,
        [FromBody] CreateLabelRequest request,
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

        _logger.LogInformation("Creating label '{LabelName}' for project {ProjectId}", request.Name, projectId);
        var label = await _labelService.CreateLabelAsync(projectId, request, cancellationToken);

        return CreatedAtAction(nameof(GetLabel), new { projectId, labelId = label.Id },
            ApiResponse<LabelResponse>.SuccessResponse(label, "Label created successfully"));
    }

    /// <summary>
    /// Update an existing label.
    /// </summary>
    [HttpPut("{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLabel(
        Guid projectId,
        Guid labelId,
        [FromBody] UpdateLabelRequest request,
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

        _logger.LogInformation("Updating label {LabelId}", labelId);
        var label = await _labelService.UpdateLabelAsync(labelId, request, cancellationToken);

        if (label.ProjectId != projectId)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Label not found in this project",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Label not found in this project" } }
            ));
        }

        return Ok(ApiResponse<LabelResponse>.SuccessResponse(label, "Label updated successfully"));
    }

    /// <summary>
    /// Delete a label.
    /// </summary>
    [HttpDelete("{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel(
        Guid projectId,
        Guid labelId,
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

        _logger.LogInformation("Deleting label {LabelId}", labelId);
        var success = await _labelService.DeleteLabelAsync(labelId, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Label not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Label not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }, "Label deleted successfully"));
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
