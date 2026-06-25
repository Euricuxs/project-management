using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Label.
/// </summary>
public class LabelConfiguration : IEntityTypeConfiguration<DomainEntities.Label>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Label> builder)
    {
        builder.ToTable("Labels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProjectId)
            .IsRequired();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Color)
            .IsRequired()
            .HasMaxLength(7)
            .HasDefaultValue("#6b7280");

        // Relationship with Project
        builder.HasOne(l => l.Project)
            .WithMany(p => p.Labels)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on ProjectId + Name (case-insensitive via collation)
        builder.HasIndex(l => new { l.ProjectId, l.Name })
            .IsUnique()
            .HasFilter(null); // Note: SQLite doesn't support case-insensitive unique indexes well

        // Additional indexes
        builder.HasIndex(l => l.ProjectId);
        builder.HasIndex(l => l.IsDeleted);
    }
}
