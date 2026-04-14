using Domain.Enumerations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using DomainTask = Domain.Entities.Task;

namespace Infrastructure.Persistence.Configurations;

public class TaskConfiguration : BaseEntityConfiguration<DomainTask>
{
    public override void Configure(EntityTypeBuilder<DomainTask> builder)
    {
        base.Configure(builder);

        builder.ToTable("Tasks");

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(t => t.AssignedUserId)
            .IsRequired(false);

        builder.Property(t => t.OrderIndex)
            .IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");

        builder.Property(t => t.DueDate)
            .IsRequired(false);

        builder.HasOne(t => t.Board)
            .WithMany(b => b.Tasks)
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.List)
            .WithMany(l => l.Tasks)
            .HasForeignKey(t => t.ListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Status)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
