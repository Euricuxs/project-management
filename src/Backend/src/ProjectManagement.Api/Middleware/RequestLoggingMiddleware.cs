namespace ProjectManagement.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        context.Items["CorrelationId"] = correlationId;

        _logger.LogInformation(
            "HTTP {Method} {Path} started. CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                correlationId);
        }
    }
}
