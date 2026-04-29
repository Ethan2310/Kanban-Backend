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
    private readonly IAdminAuthorizationService _adminAuthorizationService;
    private readonly IValidator<CreateBoardRequest> _createBoardValidator;

    private readonly IValidator<UpdateBoardRequest> _updateBoardValidator;
    private readonly IValidator<DeleteBoardRequest> _deleteBoardValidator;
    private readonly IValidator<GetBoardsRequest> _getBoardsValidator;

    public BoardService(
        IApplicationDbContext context,
        IAdminAuthorizationService adminAuthorizationService,
        IValidator<CreateBoardRequest> createBoardValidator,
        IValidator<UpdateBoardRequest> updateBoardValidator,
        IValidator<DeleteBoardRequest> deleteBoardValidator,
        IValidator<GetBoardsRequest> getBoardsValidator)
    {
        _context = context;
        _adminAuthorizationService = adminAuthorizationService;
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

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "create", "boards", ct);

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId && p.IsActive, ct);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var board = new Domain.Entities.Board
        {
            Name = request.Name,
            Description = request.Description,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Boards.Add(board);
        await _context.SaveChangesAsync(ct);

        var projectBoard = new Domain.Entities.ProjectBoard
        {
            ProjectId = request.ProjectId,
            BoardId = board.Id,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        _context.ProjectBoards.Add(projectBoard);
        await _context.SaveChangesAsync(ct);

        return new CreateBoardResponse(board.Id, board.Name, board.Description);
    }

    public async Task<UpdateBoardResponse> UpdateBoardAsync(int boardId, UpdateBoardRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "update", "boards", ct);

        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardId, ct) ?? throw new NotFoundException("Board", boardId);

        board.UpdatedById = currentUserId;
        board.UpdatedOn = DateTime.UtcNow;

        if (request.Name != null)
            board.Name = request.Name;
        if (request.Description != null)
            board.Description = request.Description;

        await _context.SaveChangesAsync(ct);

        return new UpdateBoardResponse(board.Id, board.Name, board.Description);
    }

    public async Task<DeleteBoardResponse> DeleteBoardAsync(DeleteBoardRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _deleteBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "delete", "boards", ct);

        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardID, ct) ?? throw new NotFoundException("Board", request.BoardID);

        board.IsActive = false;

        var projectBoardLinks = await _context.ProjectBoards
            .Where(pb => pb.BoardId == request.BoardID && pb.IsActive)
            .ToListAsync(ct);

        foreach (var projectBoardLink in projectBoardLinks)
        {
            projectBoardLink.IsActive = false;
            projectBoardLink.UpdatedById = currentUserId;
            projectBoardLink.UpdatedOn = DateTime.UtcNow;
        }

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
