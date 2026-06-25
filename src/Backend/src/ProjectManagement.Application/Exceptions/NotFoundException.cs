namespace ProjectManagement.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : ApplicationException
{
    public NotFoundException(string entityName, object key)
        : base(
            $"{entityName} with key '{key}' was not found.",
            "NOT_FOUND",
            $"Entity: {entityName}, Key: {key}")
    {
    }

    public NotFoundException(string message)
        : base(message, "NOT_FOUND")
    {
    }
}
