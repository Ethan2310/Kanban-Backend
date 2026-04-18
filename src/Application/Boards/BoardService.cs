using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;


namespace Application.Boards;

public class BoardService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateBoardRequest> _createBoardValidator;

    private readonly IValidator<UpdateBoardRequest> _updateBoardValidator;
    private readonly IValidator<DeleteBoardRequest> _deleteBoardValidator;
    private readonly IValidator<GetBoardsRequest> _getBoardsValidator;

    public BoardService(
        IApplicationDbContext context,
        IValidator<CreateBoardRequest> createBoardValidator,
        IValidator<UpdateBoardRequest> updateBoardValidator,
        IValidator<DeleteBoardRequest> deleteBoardValidator,
        IValidator<GetBoardsRequest> getBoardsValidator)
    {
        _context = context;
        _createBoardValidator = createBoardValidator;
        _updateBoardValidator = updateBoardValidator;
        _deleteBoardValidator = deleteBoardValidator;
        _getBoardsValidator = getBoardsValidator;
    }

    public async Task<CreateBoardResponse> CreateBoardAsync(CreateBoardRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _createBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var addedByUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (addedByUser is null || addedByUser.Role != Domain.Enumerations.UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to create boards.");

        var board = new Domain.Entities.Board
        {
            Name = request.Name,
            Description = request.Description,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Boards.Add(board);
        await _context.SaveChangesAsync(ct);

        return new CreateBoardResponse(board.Id, board.Name, board.Description);
    }

    public async Task<UpdateBoardResponse> UpdateBoardAsync(UpdateBoardRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (currentUser is null || currentUser.Role != Domain.Enumerations.UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to update boards.");

        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct) ?? throw new NotFoundException(request.BoardId.ToString(), request.BoardId);

        board.UpdatedById = currentUserId;
        board.UpdatedOn = DateTime.UtcNow;
        board.Name = request.Name;
        board.Description = request.Description;

        await _context.SaveChangesAsync(ct);

        return new UpdateBoardResponse(board.Id, board.Name, board.Description);
    }

    public async Task<DeleteBoardResponse> DeleteBoardAsync(DeleteBoardRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _deleteBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (currentUser is null || currentUser.Role != Domain.Enumerations.UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to delete boards.");

        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardID, ct) ?? throw new NotFoundException(request.BoardID.ToString(), request.BoardID);

        board.IsActive = false;
        await _context.SaveChangesAsync(ct);

        return new DeleteBoardResponse(true);
    }

    public async Task<GetBoardsResponse> GetBoardsAsync(GetBoardsRequest request, CancellationToken ct)
    {
        var validation = await _getBoardsValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var query = _context.Boards
            .Where(b => b.IsActive);

        if (request.ProjectId.HasValue)
            query = query.Where(b => b.ProjectBoards.Any(pb => pb.IsActive && pb.ProjectId == request.ProjectId.Value));

        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(b => b.Name.Contains(request.Name));

        var totalCount = await query.CountAsync(ct);
        var boards = await query
            .OrderByDescending(b => b.CreatedOn)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BoardSummaryResponse(b.Id, b.Name, b.Description))
            .ToListAsync(ct);

        return new GetBoardsResponse(boards, new PaginationMetadata(totalCount, request.PageNumber, request.PageSize));
    }

}
