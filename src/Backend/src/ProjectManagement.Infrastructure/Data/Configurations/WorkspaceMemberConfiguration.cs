using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for WorkspaceMember.
/// </summary>
public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<DomainEntities.WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<DomainEntities.WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");

        builder.HasKey(wm => wm.Id);

        builder.Property(wm => wm.WorkspaceId)
            .IsRequired();

        builder.Property(wm => wm.UserId)
            .IsRequired();

        // Composite unique index to prevent duplicate memberships
        builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .IsUnique();

        // Relationships
        builder.HasOne(wm => wm.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wm => wm.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(wm => wm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
