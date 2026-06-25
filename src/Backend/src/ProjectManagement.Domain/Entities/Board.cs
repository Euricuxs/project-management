namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Board entity - represents a Kanban board within a project.
/// </summary>
public class Board : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Enums.BoardType Type { get; set; } = Enums.BoardType.Kanban;
    public int Position { get; set; } = 0;
    public bool IsDefault { get; set; } = false;

    // Navigation properties
    public Project? Project { get; set; }
    public ICollection<Column> Columns { get; set; } = new List<Column>();
}
