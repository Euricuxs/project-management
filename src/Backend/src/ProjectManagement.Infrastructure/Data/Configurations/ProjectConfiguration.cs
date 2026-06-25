using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Project.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<DomainEntities.Project>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.WorkspaceId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        builder.Property(p => p.Key)
            .HasMaxLength(10);

        builder.HasIndex(p => p.Key)
            .IsUnique();

        builder.Property(p => p.IconUrl)
            .HasMaxLength(1024);

        builder.Property(p => p.Color)
            .HasMaxLength(7)
            .HasDefaultValue("#3b82f6");

        // Relationship with Workspace
        builder.HasOne(p => p.Workspace)
            .WithMany(w => w.Projects)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Status);
    }
}
