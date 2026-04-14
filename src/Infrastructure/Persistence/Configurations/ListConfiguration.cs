using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using DomainList = Domain.Entities.List;

namespace Infrastructure.Persistence.Configurations;

public class ListConfiguration : BaseEntityConfiguration<DomainList>
{
    public override void Configure(EntityTypeBuilder<DomainList> builder)
    {
        base.Configure(builder);

        builder.ToTable("Lists");

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.OrderIndex)
            .IsRequired();

        builder.HasOne(l => l.Board)
            .WithMany(b => b.Lists)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Status)
            .WithMany(s => s.Lists)
            .HasForeignKey(l => l.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
