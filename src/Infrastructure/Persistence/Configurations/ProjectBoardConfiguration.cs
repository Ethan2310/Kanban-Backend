using Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProjectBoardConfiguration : BaseEntityConfiguration<ProjectBoard>
{
    public override void Configure(EntityTypeBuilder<ProjectBoard> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProjectBoards");

        // Enforce uniqueness on the join pair (BaseEntity.Id remains the PK)
        builder.HasIndex(pb => new { pb.ProjectId, pb.BoardId })
            .IsUnique();

        builder.HasOne(pb => pb.Project)
            .WithMany(p => p.ProjectBoards)
            .HasForeignKey(pb => pb.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pb => pb.Board)
            .WithMany(b => b.ProjectBoards)
            .HasForeignKey(pb => pb.BoardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
