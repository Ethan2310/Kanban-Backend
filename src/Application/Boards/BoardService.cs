using Application.Common.Exceptions;
using Application.Common.Interfaces;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;


namespace Application.Boards;

public class BoardService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateBoardRequest> _createBoardValidator;

    public BoardService(IApplicationDbContext context, IValidator<CreateBoardRequest> createBoardValidator)
    {
        _context = context;
        _createBoardValidator = createBoardValidator;
    }

    public async Task<CreateBoardResponse> CreateBoardAsync(CreateBoardRequest request, CancellationToken ct)
    {
        var validation = await _createBoardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var addedByUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, ct);

        if (addedByUser is null || addedByUser.Role != Domain.Enumerations.UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to create boards.");

        var board = new Domain.Entities.Board
        {
            Name = request.Name,
            Description = request.Description,
            CreatedById = request.AdminId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Boards.Add(board);
        await _context.SaveChangesAsync(ct);

        return new CreateBoardResponse(board.Id, board.Name, board.Description);
    }
}
