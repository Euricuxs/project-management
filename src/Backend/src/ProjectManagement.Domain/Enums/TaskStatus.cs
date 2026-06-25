namespace ProjectManagement.Domain.Enums;

/// <summary>
/// Represents the status of a task.
/// </summary>
public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3,
    Cancelled = 4
}
