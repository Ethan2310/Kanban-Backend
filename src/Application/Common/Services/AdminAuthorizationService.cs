using Application.Common.Exceptions;
using Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Application.Common.Services;

public class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly IApplicationDbContext _context;

    public AdminAuthorizationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureAdminUserAsync(int userId, string action, string resource, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.Role != Domain.Enumerations.UserRole.Admin)
            throw new UnauthorizedException($"You do not have permission to {action} {resource}.");
    }
}
