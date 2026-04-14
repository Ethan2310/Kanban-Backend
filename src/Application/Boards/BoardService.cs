using Application.Common.Interfaces;

namespace Application.Boards;

public class BoardService
{
    private readonly IApplicationDbContext _context;

    public BoardService(IApplicationDbContext context) => _context = context;
}
