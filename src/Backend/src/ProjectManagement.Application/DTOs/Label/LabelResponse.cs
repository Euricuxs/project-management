namespace ProjectManagement.Application.DTOs.Label;

/// <summary>
/// Response model for a single label.
/// </summary>
public class LabelResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response model for a label list item (without full details).
/// </summary>
public class LabelListItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
