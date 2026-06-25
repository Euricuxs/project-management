namespace ProjectManagement.Application.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : ApplicationException
{
    public BusinessRuleException(string message)
        : base(message, "BUSINESS_RULE_VIOLATION")
    {
    }

    public BusinessRuleException(string message, string code)
        : base(message, code)
    {
    }
}
