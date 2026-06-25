using ProjectManagement.Application.Common.Models;

namespace ProjectManagement.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            Application.Exceptions.ApplicationException appEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse.ErrorResponse(appEx.Message, new List<ApiError>
                {
                    new() { Code = appEx.Code, Message = appEx.Message }
                })
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An unexpected error occurred. Please try again later.")
            )
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
