namespace ProjectManagement.Domain.Enums;

/// <summary>
/// Types of activities that can be tracked in the system.
/// </summary>
public enum ActivityType
{
    // Task activities
    TaskCreated = 1,
    TaskUpdated = 2,
    TaskDeleted = 3,
    TaskMoved = 4,
    TaskAssigned = 5,
    TaskUnassigned = 6,

    // Task comment activities
    TaskCommentCreated = 10,
    TaskCommentUpdated = 11,
    TaskCommentDeleted = 12,

    // Label activities
    LabelAddedToTask = 20,
    LabelRemovedFromTask = 21,

    // Project activities
    ProjectCreated = 100,
    ProjectUpdated = 101,
    ProjectDeleted = 102,
    ProjectArchived = 103,
    ProjectRestored = 104,

    // Board activities
    BoardCreated = 200,
    BoardUpdated = 201,
    BoardDeleted = 202,

    // Column activities
    ColumnCreated = 300,
    ColumnUpdated = 301,
    ColumnDeleted = 302,

    // Workspace activities
    WorkspaceCreated = 400,
    WorkspaceUpdated = 401,
    WorkspaceDeleted = 402,
    MemberAdded = 403,
    MemberRemoved = 404,
    MemberRoleChanged = 405,
}
