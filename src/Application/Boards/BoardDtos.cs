namespace Application.Boards;

public record CreateBoardRequest(
    int AdminId,
    string Name,
    string? Description);

public record CreateBoardResponse(
    int BoardId,
    string Name,
    string? Description);
