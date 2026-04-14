using System.Linq.Expressions;

using Application.Common.Interfaces;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using DomainList = Domain.Entities.List;
using DomainTask = Domain.Entities.Task;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<ProjectBoard> ProjectBoards => Set<ProjectBoard>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<DomainList> Lists => Set<DomainList>();
    public DbSet<DomainTask> Tasks => Set<DomainTask>();
    public DbSet<UserProjectAccess> UserProjectAccesses => Set<UserProjectAccess>();
    public DbSet<TaskStatusHistory> TaskStatusHistories => Set<TaskStatusHistory>();

    public override System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken ct) => base.SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply global soft-delete filter to every entity that inherits BaseEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsActive));
                var filter = Expression.Lambda(property, parameter);
                entityType.SetQueryFilter(filter);
            }
        }
    }
}
