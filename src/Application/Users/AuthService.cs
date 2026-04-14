using Application.Common.Interfaces;

namespace Application.Users;

public class AuthService
{
    private readonly IApplicationDbContext _context;

    public AuthService(IApplicationDbContext context) => _context = context;
}
