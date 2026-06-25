using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Extensions;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Auth;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate a user with email and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens and user info</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                result.ErrorMessage!,
                new List<ApiError> { new() { Code = result.ErrorCode!, Message = result.ErrorMessage! } }
            ));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result.Data!, "Login successful"));
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens and user info</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "EMAIL_EXISTS")
            {
                return Conflict(ApiResponse<object>.ErrorResponse(
                    result.ErrorMessage!,
                    new List<ApiError> { new() { Code = result.ErrorCode, Message = result.ErrorMessage! } }
                ));
            }
            return BadRequest(ApiResponse<object>.ErrorResponse(
                result.ErrorMessage!,
                new List<ApiError> { new() { Code = result.ErrorCode!, Message = result.ErrorMessage! } }
            ));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result.Data!, "Registration successful"));
    }

    /// <summary>
    /// Refresh access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                result.ErrorMessage!,
                new List<ApiError> { new() { Code = result.ErrorCode!, Message = result.ErrorMessage! } }
            ));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result.Data!, "Token refreshed successfully"));
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var success = await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

        return Ok(ApiResponse<LogoutResponse>.SuccessResponse(new LogoutResponse
        {
            Success = success,
            Message = success ? "Logged out successfully" : "Logout failed"
        }));
    }

    /// <summary>
    /// Get current user information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var user = await _authService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "User not found",
                new List<ApiError> { new() { Code = "NOT_FOUND", Message = "User not found" } }
            ));
        }

        return Ok(ApiResponse<UserResponse>.SuccessResponse(user));
    }

    /// <summary>
    /// Revoke all refresh tokens for the current user (sign out from all devices).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation</returns>
    [HttpPost("revoke-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllTokens(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "User not authenticated",
                new List<ApiError> { new() { Code = "UNAUTHORIZED", Message = "User not authenticated" } }
            ));
        }

        var success = await _authService.RevokeAllUserTokensAsync(userId.Value, cancellationToken);

        return Ok(ApiResponse<LogoutResponse>.SuccessResponse(new LogoutResponse
        {
            Success = success,
            Message = success ? "All sessions terminated" : "Failed to revoke sessions"
        }));
    }
}
