namespace ProjectManagement.Application.Exceptions;

/// <summary>
/// Exception thrown when an entity already exists.
/// </summary>
public class AlreadyExistsException : ApplicationException
{
    public AlreadyExistsException(string entityName, string propertyName, object value)
        : base(
            $"{entityName} with {propertyName} '{value}' already exists.",
            "ALREADY_EXISTS",
            $"Entity: {entityName}, Property: {propertyName}, Value: {value}")
    {
    }

    public AlreadyExistsException(string message)
        : base(message, "ALREADY_EXISTS")
    {
    }
}
