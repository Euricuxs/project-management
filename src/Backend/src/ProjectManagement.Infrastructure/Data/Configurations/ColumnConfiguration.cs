using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntities = ProjectManagement.Domain.Entities;

namespace ProjectManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Column.
/// </summary>
public class ColumnConfiguration : IEntityTypeConfiguration<DomainEntities.Column>
{
    public void Configure(EntityTypeBuilder<DomainEntities.Column> builder)
    {
        builder.ToTable("Columns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BoardId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Position)
            .HasDefaultValue(0);

        builder.Property(c => c.WipLimit)
            .HasDefaultValue(0);

        builder.Property(c => c.Color)
            .HasMaxLength(7)
            .HasDefaultValue("#6b7280");

        builder.Property(c => c.TaskStatus)
            .HasConversion<int?>();

        // Relationship with Board
        builder.HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for ordering columns within a board
        builder.HasIndex(c => new { c.BoardId, c.Position });
    }
}
