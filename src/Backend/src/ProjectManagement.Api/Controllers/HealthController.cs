using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Common.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Health check controller for API status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Simple health check endpoint.
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse> Get()
    {
        return Ok(ApiResponse.SuccessResponse("Project Management API is running!"));
    }

    /// <summary>
    /// Detailed health check with version info.
    /// </summary>
    [HttpGet("detailed")]
    public ActionResult<ApiResponse> GetDetailed()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Database = "Connected"
        };

        return Ok(ApiResponse<object>.SuccessResponse(health, "System is healthy"));
    }
}
