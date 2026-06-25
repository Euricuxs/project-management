using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Board;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for board operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;
    private readonly ILogger<BoardsController> _logger;

    public BoardsController(IBoardService boardService, ILogger<BoardsController> logger)
    {
        _boardService = boardService;
        _logger = logger;
    }

    /// <summary>
    /// Get all boards for a project.
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BoardListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProjectBoards(
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

        var boards = await _boardService.GetProjectBoardsAsync(projectId, userId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BoardListItemResponse>>.SuccessResponse(boards));
    }

    /// <summary>
    /// Get a single board by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoard(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var board = await _boardService.GetBoardByIdAsync(id, userId.Value, cancellationToken);
        if (board == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Board not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Board not found" } }
            ));
        }

        return Ok(ApiResponse<BoardResponse>.SuccessResponse(board));
    }

    /// <summary>
    /// Create a new board.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        _logger.LogInformation("Creating board '{BoardName}' for project {ProjectId}", request.Name, request.ProjectId);
        var board = await _boardService.CreateBoardAsync(request, userId.Value, cancellationToken);

        return CreatedAtAction(nameof(GetBoard), new { id = board.Id },
            ApiResponse<BoardResponse>.SuccessResponse(board, "Board created successfully"));
    }

    /// <summary>
    /// Update an existing board.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBoard(Guid id, [FromBody] UpdateBoardRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        _logger.LogInformation("Updating board {BoardId}", id);
        var board = await _boardService.UpdateBoardAsync(id, request, userId.Value, cancellationToken);

        return Ok(ApiResponse<BoardResponse>.SuccessResponse(board, "Board updated successfully"));
    }

    /// <summary>
    /// Delete a board.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBoard(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        _logger.LogInformation("Deleting board {BoardId}", id);
        var success = await _boardService.DeleteBoardAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "Board not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "Board not found" } }
            ));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }, "Board deleted successfully"));
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
