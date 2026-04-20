using Application.Common.Models;

using Domain.Enumerations;

namespace Application.Tasks;

public record CreateTaskRequest(string Title, string? Description, int BoardId, int ListId, int StatusId, int AssignedUserId, int OrderIndex, TaskPriority Priority, DateTime? DueDate);

public record CreateTaskResponse(int TaskId, string Title, string? Description, int BoardId, int ListId, int StatusId, int AssignedUserId, int OrderIndex, TaskPriority Priority, DateTime? DueDate);

public record UpdateTaskRequest(string? Title, string? Description, int? StatusId, int? AssignedUserId, int? OrderIndex, TaskPriority? Priority, DateTime? DueDate);

public record UpdateTaskResponse(int TaskId, string Title, string? Description, int BoardId, int ListId, int StatusId, int AssignedUserId, int OrderIndex, TaskPriority Priority, DateTime? DueDate);

public record DeleteTaskRequest(int TaskId);

public record DeleteTaskResponse(bool Success);

public record GetTasksRequest(int? BoardId, int? ListId, int? StatusId, int? AssignedUserId, int PageNumber = PaginationRequestDefaults.PageNumber, int PageSize = PaginationRequestDefaults.PageSize);

public record TaskSummaryResponse(int TaskId, string Title, string? Description, int BoardId, int ListId, int StatusId, int AssignedUserId, int OrderIndex, TaskPriority Priority, DateTime? DueDate);

public record GetTasksResponse(IReadOnlyList<TaskSummaryResponse> Tasks, PaginationMetadata Pagination);
