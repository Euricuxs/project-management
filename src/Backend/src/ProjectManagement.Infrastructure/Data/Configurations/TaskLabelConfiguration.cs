using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for TaskLabel (join table).
/// </summary>
public class TaskLabelConfiguration : IEntityTypeConfiguration<DomainEntities.TaskLabel>
{
    public void Configure(EntityTypeBuilder<DomainEntities.TaskLabel> builder)
    {
        builder.ToTable("TaskLabels");

        // Composite primary key
        builder.HasKey(tl => new { tl.TaskId, tl.LabelId });

        // Relationship with TaskItem
        builder.HasOne(tl => tl.Task)
            .WithMany(t => t.TaskLabels)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Label
        builder.HasOne(tl => tl.Label)
            .WithMany(l => l.TaskLabels)
            .HasForeignKey(tl => tl.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(tl => tl.TaskId);
        builder.HasIndex(tl => tl.LabelId);
    }
}
