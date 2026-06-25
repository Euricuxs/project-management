using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for TaskItem.
/// </summary>
public class TaskItemConfiguration : IEntityTypeConfiguration<Domain.Entities.TaskItem>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ColumnId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .HasMaxLength(10000);

        builder.Property(t => t.TaskKey)
            .HasMaxLength(20);

        builder.HasIndex(t => t.TaskKey)
            .IsUnique();

        builder.Property(t => t.Position)
            .HasDefaultValue(0);

        builder.Property(t => t.EstimatedHours)
            .HasDefaultValue(0);

        builder.Property(t => t.ActualHours)
            .HasDefaultValue(0);

        builder.Property(t => t.ReporterId)
            .IsRequired();

        // Relationship with Column
        builder.HasOne(t => t.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Assignee (optional)
        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey("AssigneeId")
            .OnDelete(DeleteBehavior.SetNull);

        // Relationship with Reporter
        builder.HasOne(t => t.Reporter)
            .WithMany()
            .HasForeignKey("ReporterId")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with TaskLabels (many-to-many with Label)
        builder.HasMany(t => t.TaskLabels)
            .WithOne(tl => tl.Task)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => new { t.ColumnId, t.Position });
        builder.HasIndex("AssigneeId");
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.DueDate);
    }
}
