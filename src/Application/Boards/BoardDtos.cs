namespace Application.Boards;

public record CreateBoardRequest(
    string Name,
    string? Description);

public record CreateBoardResponse(
    int BoardId,
    string Name,
    string? Description);
