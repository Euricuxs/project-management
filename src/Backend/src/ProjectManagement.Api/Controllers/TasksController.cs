using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Label;
using ProjectManagement.Application.DTOs.Task;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for task operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILabelService _labelService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILabelService labelService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _labelService = labelService;
        _logger = logger;
    }

    /// <summary>
    /// Get a task by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var task = await _taskService.GetTaskByIdAsync(id, userId.Value, cancellationToken);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Task not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Task not found" } }
            ));

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(task));
    }

    /// <summary>
    /// Get all tasks for a column.
    /// </summary>
    [HttpGet("column/{columnId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasksByColumn(Guid columnId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var tasks = await _taskService.GetTasksByColumnAsync(columnId, userId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TaskListItemResponse>>.SuccessResponse(tasks));
    }

    /// <summary>
    /// Get all tasks for a board.
    /// </summary>
    [HttpGet("board/{boardId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasksByBoard(Guid boardId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var tasks = await _taskService.GetTasksByBoardAsync(boardId, userId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TaskListItemResponse>>.SuccessResponse(tasks));
    }

    /// <summary>
    /// Create a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        _logger.LogInformation("Creating task '{TaskTitle}' for column {ColumnId}", request.Title, request.ColumnId);

        try
        {
            var task = await _taskService.CreateTaskAsync(request, userId.Value, cancellationToken);

            return CreatedAtAction(nameof(GetTask), new { id = task.Id },
                ApiResponse<TaskResponse>.SuccessResponse(task, "Task created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task: {Message}", ex.Message);

            // Extract inner exception message for better debugging
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var fullError = $"Failed to create task: {innerMessage}";

            _logger.LogError(ex, "Inner exception: {InnerEx}", ex.InnerException?.Message);
            if (ex.InnerException?.InnerException != null)
            {
                _logger.LogError("Deep inner exception: {DeepInnerEx}", ex.InnerException.InnerException.Message);
                fullError += $" | {ex.InnerException.InnerException.Message}";
            }

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                fullError,
                new List<ApiError> { new() { Code = "INTERNAL_ERROR", Message = fullError } }
            ));
        }
    }

    /// <summary>
    /// Update an existing task.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        _logger.LogInformation("Updating task {TaskId}", id);

        var task = await _taskService.UpdateTaskAsync(id, request, userId.Value, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(task, "Task updated successfully"));
    }

    /// <summary>
    /// Delete a task (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        _logger.LogInformation("Deleting task {TaskId}", id);

        await _taskService.DeleteTaskAsync(id, userId.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }, "Task deleted successfully"));
    }

    /// <summary>
    /// Move a task to a different column/position.
    /// </summary>
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        _logger.LogInformation("Moving task {TaskId} to column {TargetColumnId} at position {Position}", id, request.TargetColumnId, request.TargetPosition);

        var task = await _taskService.MoveTaskAsync(id, request.TargetColumnId, request.TargetPosition, userId.Value, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(task, "Task moved successfully"));
    }

    /// <summary>
    /// Get all labels for a task.
    /// </summary>
    [HttpGet("{taskId:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabelListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskLabels(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var task = await _taskService.GetTaskByIdAsync(taskId, userId.Value, cancellationToken);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Task not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Task not found" } }
            ));

        var labels = await _labelService.GetTaskLabelsAsync(taskId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LabelListItemResponse>>.SuccessResponse(labels));
    }

    /// <summary>
    /// Add labels to a task.
    /// </summary>
    [HttpPost("{taskId:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddLabelsToTask(Guid taskId, [FromBody] AddLabelsToTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var task = await _taskService.GetTaskByIdAsync(taskId, userId.Value, cancellationToken);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Task not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Task not found" } }
            ));

        _logger.LogInformation("Adding labels to task {TaskId}", taskId);
        await _labelService.AddLabelsToTaskAsync(taskId, request, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { added = true }, "Labels added successfully"));
    }

    /// <summary>
    /// Remove a label from a task.
    /// </summary>
    [HttpDelete("{taskId:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLabelFromTask(Guid taskId, Guid labelId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));

        var task = await _taskService.GetTaskByIdAsync(taskId, userId.Value, cancellationToken);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Task not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Task not found" } }
            ));

        _logger.LogInformation("Removing label {LabelId} from task {TaskId}", labelId, taskId);
        var success = await _labelService.RemoveLabelFromTaskAsync(taskId, labelId, cancellationToken);

        if (!success)
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Label assignment not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Label assignment not found" } }
            ));

        return Ok(ApiResponse<object>.SuccessResponse(new { removed = true }, "Label removed successfully"));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }
}

/// <summary>
/// Request model for moving a task.
/// </summary>
public class MoveTaskRequest
{
    public Guid TargetColumnId { get; set; }
    public int TargetPosition { get; set; }
}
