using Application.Common.Interfaces;

namespace Application.Statuses;

public class StatusService
{
    private readonly IApplicationDbContext _context;

    public StatusService(IApplicationDbContext context) => _context = context;
}
