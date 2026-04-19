using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

using Domain.ValueObjects;

using FluentValidation;

using Microsoft.EntityFrameworkCore;


using ValidationException = Application.Common.Exceptions.ValidationException;


namespace Application.Statuses;

public class StatusService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateStatusRequest> _createStatusValidator;
    private readonly IValidator<UpdateStatusRequest> _updateStatusValidator;
    private readonly IValidator<DeleteStatusRequest> _deleteStatusValidator;
    private readonly IValidator<GetStatusesRequest> _getStatusesValidator;
    private readonly IAdminAuthorizationService _adminAuthorizationService;

    public StatusService(
        IApplicationDbContext context,
        IAdminAuthorizationService adminAuthorizationService,
        IValidator<CreateStatusRequest> createStatusValidator,
        IValidator<UpdateStatusRequest> updateStatusValidator,
        IValidator<DeleteStatusRequest> deleteStatusValidator,
        IValidator<GetStatusesRequest> getStatusesValidator)
    {
        _context = context;
        _adminAuthorizationService = adminAuthorizationService;
        _createStatusValidator = createStatusValidator;
        _updateStatusValidator = updateStatusValidator;
        _deleteStatusValidator = deleteStatusValidator;
        _getStatusesValidator = getStatusesValidator;
    }

    public async Task<CreateStatusResponse> CreateStatusAsync(CreateStatusRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _createStatusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "create", "statuses", ct);

        var status = new Domain.Entities.Status
        {
            Name = request.Name,
            OrderIndex = request.OrderIndex,
            Color = new HexColor(request.Color)
        };

        _context.Statuses.Add(status);
        await _context.SaveChangesAsync(ct);

        return new CreateStatusResponse(status.Id, status.Name, status.OrderIndex, RequireColor(status));
    }

    public async Task<UpdateStatusResponse> UpdateStatusAsync(int statusId, UpdateStatusRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateStatusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "update", "statuses", ct);

        var status = await _context.Statuses.FindAsync(new object[] { statusId }, ct)
            ?? throw new NotFoundException("Status", statusId);

        if (request.Name != null)
            status.Name = request.Name;
        if (request.OrderIndex.HasValue)
            status.OrderIndex = request.OrderIndex.Value;
        if (request.Color != null)
            status.Color = new HexColor(request.Color);

        await _context.SaveChangesAsync(ct);

        return new UpdateStatusResponse(status.Id, status.Name, status.OrderIndex, RequireColor(status));
    }

    public async Task<DeleteStatusResponse> DeleteStatusAsync(DeleteStatusRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _deleteStatusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "delete", "statuses", ct);

        var status = await _context.Statuses.FindAsync(new object[] { request.StatusId }, ct)
            ?? throw new NotFoundException("Status", request.StatusId);

        status.IsActive = false;
        await _context.SaveChangesAsync(ct);

        return new DeleteStatusResponse(true);
    }

    public async Task<GetStatusesResponse> GetStatusesAsync(GetStatusesRequest request, CancellationToken ct)
    {
        var validation = await _getStatusesValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var query = _context.Statuses
            .Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(s => s.Name.Contains(request.Name));

        var totalCount = await query.CountAsync(ct);

        var statuses = await query
            .OrderBy(s => s.OrderIndex)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var response = statuses
            .Select(s => new StatusSummaryResponse(s.Id, s.Name, s.OrderIndex, RequireColor(s)))
            .ToList();

        var paginationMetadata = new PaginationMetadata(totalCount, request.PageNumber, request.PageSize);

        return new GetStatusesResponse(response, paginationMetadata);
    }

    private static string RequireColor(Domain.Entities.Status status) =>
        status.Color?.Value ?? throw new InvalidOperationException($"Status '{status.Id}' is missing a color.");
}
