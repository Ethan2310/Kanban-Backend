using Domain.Entities;
using Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StatusConfiguration : BaseEntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("Statuses");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Color)
            .HasConversion(
                v => v == null ? null : v.Value,
                v => v == null ? null : new HexColor(v))
            .HasColumnType("varchar(7)")
            .IsRequired(false);

        builder.Property(s => s.OrderIndex)
            .IsRequired();
    }
}
