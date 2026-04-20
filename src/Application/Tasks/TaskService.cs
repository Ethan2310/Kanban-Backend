using System.Transactions;

using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;


namespace Application.Tasks;

public class TaskService
{
    private readonly IApplicationDbContext _context;

    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IValidator<UpdateTaskRequest> _updateTaskValidator;
    private readonly IValidator<DeleteTaskRequest> _deleteTaskValidator;
    private readonly IValidator<GetTasksRequest> _getTasksValidator;

    public TaskService(IApplicationDbContext context, IValidator<CreateTaskRequest> createTaskValidator, IValidator<UpdateTaskRequest> updateTaskValidator, IValidator<DeleteTaskRequest> deleteTaskValidator, IValidator<GetTasksRequest> getTasksValidator)
    {
        _context = context;
        _createTaskValidator = createTaskValidator;
        _updateTaskValidator = updateTaskValidator;
        _deleteTaskValidator = deleteTaskValidator;
        _getTasksValidator = getTasksValidator;
    }

    public async Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _createTaskValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var taskBoard = await _context.Boards.FindAsync(new object[] { request.BoardId }, ct)
             ?? throw new NotFoundException("Board", request.BoardId);

        var taskList = await _context.Lists.FindAsync(new object[] { request.ListId }, ct)
            ?? throw new NotFoundException("List", request.ListId);

        var taskStatus = await _context.Statuses.FindAsync(new object[] { request.StatusId }, ct)
            ?? throw new NotFoundException("Status", request.StatusId);

        var assignedUser = request.AssignedUserId.HasValue
            ? await _context.Users.FindAsync(new object[] { request.AssignedUserId.Value }, ct)
                ?? throw new NotFoundException("User", request.AssignedUserId.Value)
            : null;

        var task = new Domain.Entities.Task
        {
            Title = request.Title,
            Description = request.Description,
            BoardId = request.BoardId,
            ListId = request.ListId,
            StatusId = request.StatusId,
            AssignedUserId = request.AssignedUserId,
            OrderIndex = request.OrderIndex,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(ct);

            var taskHistory = new Domain.Entities.TaskStatusHistory
            {
                TaskId = task.Id,
                StatusChangedTo = task.StatusId,
                ChangedAt = DateTime.UtcNow,
                ChangedById = currentUserId
            };

            _context.TaskStatusHistories.Add(taskHistory);
            await _context.SaveChangesAsync(ct);

            scope.Complete();
        }

        return new CreateTaskResponse(task.Id, task.Title, task.Description, task.BoardId, task.ListId, task.StatusId, task.AssignedUserId, task.OrderIndex, task.Priority, task.DueDate);
    }

    public async Task<UpdateTaskResponse> UpdateTaskAsync(int taskId, UpdateTaskRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateTaskValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var task = await _context.Tasks.FindAsync(new object[] { taskId }, ct)
            ?? throw new NotFoundException("Task", taskId);

        var statusChangedFrom = task.StatusId;
        var statusChangedTo = task.StatusId;

        if (request.StatusId.HasValue)
        {
            var newTaskStatus = await _context.Statuses.FindAsync(new object[] { request.StatusId.Value }, ct)
                ?? throw new NotFoundException("Status", request.StatusId.Value);
            statusChangedTo = newTaskStatus.Id;
        }

        if (request.AssignedUserId.HasValue)
        {
            _ = await _context.Users.FindAsync(new object[] { request.AssignedUserId.Value }, ct)
                ?? throw new NotFoundException("User", request.AssignedUserId.Value);
        }

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            if (request.Title != null)
                task.Title = request.Title;
            if (request.Description != null)
                task.Description = request.Description;
            if (request.StatusId.HasValue)
                task.StatusId = request.StatusId.Value;
            if (request.AssignedUserId.HasValue)
                task.AssignedUserId = request.AssignedUserId.Value;
            if (request.OrderIndex.HasValue)
                task.OrderIndex = request.OrderIndex.Value;
            if (request.Priority.HasValue)
                task.Priority = request.Priority.Value;
            if (request.DueDate.HasValue)
                task.DueDate = request.DueDate;

            task.UpdatedById = currentUserId;
            task.UpdatedOn = DateTime.UtcNow;

            var taskHistory = new Domain.Entities.TaskStatusHistory
            {
                TaskId = task.Id,
                StatusChangedTo = statusChangedTo,
                StatusChangedFrom = statusChangedFrom,
                ChangedAt = DateTime.UtcNow,
                ChangedById = currentUserId
            };

            _context.TaskStatusHistories.Add(taskHistory);

            await _context.SaveChangesAsync(ct);
            scope.Complete();
        }

        return new UpdateTaskResponse(task.Id, task.Title, task.Description, task.BoardId, task.ListId, task.StatusId, task.AssignedUserId, task.OrderIndex, task.Priority, task.DueDate);

    }

    public async Task<DeleteTaskResponse> DeleteTaskAsync(DeleteTaskRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _deleteTaskValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var task = await _context.Tasks.FindAsync(new object[] { request.TaskId }, ct)
            ?? throw new NotFoundException("Task", request.TaskId);

        var taskHistories = await _context.TaskStatusHistories
            .Where(h => h.TaskId == task.Id)
            .ToListAsync(ct);

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            _context.TaskStatusHistories.RemoveRange(taskHistories);

            task.IsActive = false;
            task.UpdatedById = currentUserId;
            task.UpdatedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            scope.Complete();
        }

        return new DeleteTaskResponse(true);
    }

    public async Task<GetTasksResponse> GetTasksAsync(GetTasksRequest request, CancellationToken ct)
    {
        var validation = await _getTasksValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var query = _context.Tasks.AsNoTracking().Where(t => t.IsActive);

        if (request.BoardId.HasValue)
            query = query.Where(t => t.BoardId == request.BoardId.Value);
        if (request.ListId.HasValue)
            query = query.Where(t => t.ListId == request.ListId.Value);
        if (request.StatusId.HasValue)
            query = query.Where(t => t.StatusId == request.StatusId.Value);
        if (request.AssignedUserId.HasValue)
            query = query.Where(t => t.AssignedUserId == request.AssignedUserId.Value);

        var totalCount = await query.CountAsync(ct);
        var tasks = await query
            .OrderBy(t => t.OrderIndex)
            .ThenByDescending(t => t.CreatedOn)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TaskSummaryResponse(t.Id, t.Title, t.Description, t.BoardId, t.ListId, t.StatusId, t.AssignedUserId, t.OrderIndex, t.Priority, t.DueDate))
            .ToListAsync(ct);

        var paginationMetadata = new PaginationMetadata(totalCount, request.PageNumber, request.PageSize);

        return new GetTasksResponse(tasks, paginationMetadata);
    }
}
