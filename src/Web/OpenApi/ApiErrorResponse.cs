namespace Web.OpenApi;

public sealed record ApiErrorResponse(
    int Status,
    string Title,
    string Detail,
    string ErrorCode,
    IDictionary<string, string[]>? Errors);
