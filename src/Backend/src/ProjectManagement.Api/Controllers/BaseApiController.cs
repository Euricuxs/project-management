using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Base controller with common functionality.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current user's ID from JWT claims.
    /// </summary>
    /// <returns>The user ID if authenticated, null otherwise.</returns>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
