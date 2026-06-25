namespace ProjectManagement.Application.Common.Models;

/// <summary>
/// Standard API response wrapper for consistent responses.
/// </summary>
/// <typeparam name="T">Type of the data payload.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<ApiError>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully"
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic version for responses without data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Operation completed successfully"
        };
    }

    public new static ApiResponse ErrorResponse(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Represents an API error.
/// </summary>
public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
}
