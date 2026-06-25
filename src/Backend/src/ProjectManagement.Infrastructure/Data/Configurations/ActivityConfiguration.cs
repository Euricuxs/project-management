using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Activity (audit log).
/// Activities are immutable and should never be modified or deleted.
/// </summary>
public class ActivityConfiguration : IEntityTypeConfiguration<DomainEntities.Activity>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(a => a.Id);

        // No query filter - activities should always be visible for audit

        builder.Property(a => a.Type)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasMaxLength(500);

        builder.Property(a => a.ProjectId)
            .IsRequired();

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.OldValues)
            .HasColumnType("TEXT");

        builder.Property(a => a.NewValues)
            .HasColumnType("TEXT");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(a => a.ProjectId);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Type);
        builder.HasIndex(a => a.CreatedAt);

        // Composite index for common query pattern
        builder.HasIndex(a => new { a.ProjectId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });
    }
}
