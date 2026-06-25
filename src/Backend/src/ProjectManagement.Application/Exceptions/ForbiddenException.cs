namespace ProjectManagement.Application.Exceptions;

/// <summary>
/// Exception thrown when a user attempts an action they don't have permission for.
/// </summary>
public class ForbiddenException : ApplicationException
{
    public ForbiddenException()
        : base("You do not have permission to perform this action.", "FORBIDDEN")
    {
    }

    public ForbiddenException(string message)
        : base(message, "FORBIDDEN")
    {
    }

    public ForbiddenException(string message, string code, string? details = null)
        : base(message, code, details)
    {
    }
}
