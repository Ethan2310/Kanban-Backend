namespace Application.Common.Interfaces;

public interface IAdminAuthorizationService
{
    Task EnsureAdminUserAsync(int userId, string action, string resource, CancellationToken ct);
}
