using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Board.
/// </summary>
public class BoardConfiguration : IEntityTypeConfiguration<DomainEntities.Board>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Board> builder)
    {
        builder.ToTable("Boards");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ProjectId)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.Position)
            .HasDefaultValue(0);

        // Relationship with Project
        builder.HasOne(b => b.Project)
            .WithMany(p => p.Boards)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for ordering boards within a project
        builder.HasIndex(b => new { b.ProjectId, b.Position });
    }
}
