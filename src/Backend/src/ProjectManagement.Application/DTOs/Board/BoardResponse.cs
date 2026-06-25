namespace ProjectManagement.Application.DTOs.Board;

/// <summary>
/// Response model for a single board with columns and tasks.
/// </summary>
public class BoardResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ColumnResponse> Columns { get; set; } = new();
    public int TaskCount { get; set; }
}

/// <summary>
/// Response model for a board list item (without columns).
/// </summary>
public class BoardListItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ColumnCount { get; set; }
    public int TaskCount { get; set; }
}

/// <summary>
/// Response model for a column within a board.
/// </summary>
public class ColumnResponse
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Color { get; set; }
    public int WipLimit { get; set; }
    public string? TaskStatus { get; set; } // Optional status mapping for this column
    public int TaskCount { get; set; }
    public List<BoardTaskResponse> Tasks { get; set; } = new();
}

/// <summary>
/// Response model for a task within a column (board context).
/// </summary>
public class BoardTaskResponse
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaskKey { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BoardLabelResponse> Labels { get; set; } = new();
}

/// <summary>
/// Response model for a label within a board task.
/// </summary>
public class BoardLabelResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
