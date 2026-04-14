using Application.Common.Interfaces;

namespace Application.Tasks;

public class TaskService
{
    private readonly IApplicationDbContext _context;

    public TaskService(IApplicationDbContext context) => _context = context;
}
