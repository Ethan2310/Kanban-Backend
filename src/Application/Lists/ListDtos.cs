public record CreateListRequest(string Name, int BoardId, int StatusId);

public record CreateListResponse(int ListId, string Name, int BoardId, int StatusId, int OrderIndex);

public record UpdateListRequest(int ListId, string Name, int StatusId);

public record UpdateListResponse(int ListId, string Name, int BoardId, int StatusId, int OrderIndex);
