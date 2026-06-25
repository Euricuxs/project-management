namespace ProjectManagement.Application.Exceptions;

/// <summary>
/// Base exception for all application-level exceptions.
/// </summary>
public abstract class ApplicationException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    protected ApplicationException(string message, string code, string? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}
