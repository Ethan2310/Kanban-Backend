using Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserProjectAccessConfiguration : BaseEntityConfiguration<UserProjectAccess>
{
    public override void Configure(EntityTypeBuilder<UserProjectAccess> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserProjectAccess");

        // Enforce uniqueness on the join pair
        builder.HasIndex(upa => new { upa.UserId, upa.ProjectId })
            .IsUnique();

        builder.HasOne(upa => upa.User)
            .WithMany()
            .HasForeignKey(upa => upa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(upa => upa.Project)
            .WithMany(p => p.UserProjectAccesses)
            .HasForeignKey(upa => upa.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
