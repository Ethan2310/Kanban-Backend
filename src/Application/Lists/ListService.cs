using Application.Common.Interfaces;

namespace Application.Lists;

public class ListService
{
    private readonly IApplicationDbContext _context;

    public ListService(IApplicationDbContext context) => _context = context;
}
