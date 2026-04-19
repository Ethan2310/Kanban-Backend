using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;


namespace Application.Lists;

public class ListService
{
    private readonly IApplicationDbContext _context;

    private readonly IValidator<CreateListRequest> _createListValidator;
    private readonly IValidator<UpdateListRequest> _updateListValidator;
    private readonly IValidator<DeleteListRequest> _deleteListValidator;
    private readonly IValidator<GetListsRequest> _getListsValidator;

    public ListService(
        IApplicationDbContext context,
        IValidator<CreateListRequest> createListValidator,
        IValidator<UpdateListRequest> updateListValidator,
        IValidator<DeleteListRequest> deleteListValidator,
        IValidator<GetListsRequest> getListsValidator)
    {
        _context = context;
        _createListValidator = createListValidator;
        _updateListValidator = updateListValidator;
        _deleteListValidator = deleteListValidator;
        _getListsValidator = getListsValidator;
    }

    public async Task<CreateListResponse> CreateListAsync(CreateListRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _createListValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        _ = await _context.Boards.FindAsync(new object[] { request.BoardId }, ct)
            ?? throw new NotFoundException("Board", request.BoardId);

        var listStatus = await _context.Statuses.FindAsync(new object[] { request.StatusId }, ct)
            ?? throw new NotFoundException("Status", request.StatusId);

        var list = new Domain.Entities.List
        {
            Name = request.Name,
            BoardId = request.BoardId,
            StatusId = request.StatusId,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow,
            OrderIndex = listStatus.OrderIndex
        };

        _context.Lists.Add(list);
        await _context.SaveChangesAsync(ct);

        return new CreateListResponse(list.Id, list.Name, list.BoardId, list.StatusId, list.OrderIndex);
    }

    public async Task<UpdateListResponse> UpdateListAsync(int listId, UpdateListRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateListValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var list = await _context.Lists
            .FirstOrDefaultAsync(l => l.Id == listId, ct) ?? throw new NotFoundException("List", listId);

        if (request.Name != null)
            list.Name = request.Name;
        if (request.StatusId.HasValue)
        {
            var listStatus = await _context.Statuses.FindAsync(new object[] { request.StatusId.Value }, ct) ?? throw new NotFoundException("Status", request.StatusId.Value);
            list.StatusId = request.StatusId.Value;
            list.OrderIndex = listStatus.OrderIndex;
        }
        list.UpdatedById = currentUserId;
        list.UpdatedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return new UpdateListResponse(list.Id, list.Name, list.BoardId, list.StatusId, list.OrderIndex);

    }

    public async Task<DeleteListResponse> DeleteListAsync(DeleteListRequest request, CancellationToken ct)
    {
        var validation = await _deleteListValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var list = await _context.Lists
            .FirstOrDefaultAsync(l => l.Id == request.ListId, ct) ?? throw new NotFoundException("List", request.ListId);

        var hasTasksInList = await _context.Tasks
            .AnyAsync(t => t.ListId == request.ListId, ct);

        if (hasTasksInList)
            throw new BadRequestException("Cannot delete a list that contains tasks. Please move or delete the tasks first.");

        _context.Lists.Remove(list);
        await _context.SaveChangesAsync(ct);

        return new DeleteListResponse(true);

    }

    public async Task<GetListsResponse> GetListsAsync(GetListsRequest request, CancellationToken ct)
    {
        var validation = await _getListsValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var query = _context.Lists
            .Where(l => l.IsActive);

        if (request.BoardId.HasValue)
            query = query.Where(l => l.BoardId == request.BoardId.Value);

        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(l => l.Name.Contains(request.Name));

        var totalCount = await query.CountAsync(ct);

        var lists = await query
            .OrderBy(l => l.OrderIndex)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var response = lists
            .Select(l => new ListSummaryResponse(l.Id, l.Name, l.BoardId, l.StatusId, l.OrderIndex))
            .ToList();

        var paginationMetadata = new PaginationMetadata(totalCount, request.PageNumber, request.PageSize);

        return new GetListsResponse(response, paginationMetadata);
    }
}
