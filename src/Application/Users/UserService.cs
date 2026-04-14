using Application.Common.Interfaces;

namespace Application.Users;

public class UserService
{
    private readonly IApplicationDbContext _context;

    public UserService(IApplicationDbContext context) => _context = context;
}
