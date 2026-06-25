using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Workspace.
/// </summary>
public class WorkspaceConfiguration : IEntityTypeConfiguration<DomainEntities.Workspace>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(w => w.Name);

        builder.Property(w => w.Description)
            .HasMaxLength(2000);

        builder.Property(w => w.LogoUrl)
            .HasMaxLength(1024);

        builder.Property(w => w.OwnerId)
            .IsRequired();

        // Self-referencing navigation is optional - Owner is just a UserId reference
        builder.HasOne(w => w.Owner)
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.IsPublic);
    }
}
