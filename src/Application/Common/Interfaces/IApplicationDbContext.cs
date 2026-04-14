using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using DomainList = Domain.Entities.List;
using DomainTask = Domain.Entities.Task;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<Board> Boards { get; }
    DbSet<ProjectBoard> ProjectBoards { get; }
    DbSet<Status> Statuses { get; }
    DbSet<DomainList> Lists { get; }
    DbSet<DomainTask> Tasks { get; }
    DbSet<UserProjectAccess> UserProjectAccesses { get; }
    DbSet<TaskStatusHistory> TaskStatusHistories { get; }

    System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken ct);
}
