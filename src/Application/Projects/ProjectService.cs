using Application.Common.Interfaces;

namespace Application.Projects;

public class ProjectService
{
    private readonly IApplicationDbContext _context;

    public ProjectService(IApplicationDbContext context) => _context = context;
}
