using Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using DomainTask = Domain.Entities.Task;

namespace Infrastructure.Persistence.Configurations;

public class TaskStatusHistoryConfiguration : IEntityTypeConfiguration<TaskStatusHistory>
{
    public void Configure(EntityTypeBuilder<TaskStatusHistory> builder)
    {
        builder.ToTable("TaskStatusHistory");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ChangedAt)
            .IsRequired();

        builder.Property(t => t.StatusChangedFrom)
            .IsRequired(false);

        // FK columns — navigations are intentionally omitted to prevent soft-delete
        // query filters on Task/Status/User from hiding audit rows.
        builder.Property(t => t.TaskId).IsRequired();
        builder.Property(t => t.StatusChangedTo).IsRequired();
        builder.Property(t => t.ChangedById).IsRequired();
    }
}
