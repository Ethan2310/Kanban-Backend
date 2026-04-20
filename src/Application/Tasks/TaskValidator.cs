using System.Data;

using Application.Tasks;

using FluentValidation;

public class RequestCreateTaskValidator : AbstractValidator<CreateTaskRequest>
{
    public RequestCreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null);

        RuleFor(x => x.BoardId)
            .GreaterThan(0);

        RuleFor(x => x.ListId)
            .GreaterThan(0);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);

        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0)
            .When(x => x.AssignedUserId.HasValue);

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue);
    }
}

public class RequestUpdateTaskValidator : AbstractValidator<UpdateTaskRequest>
{
    public RequestUpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null);

        RuleFor(x => x.StatusId)
            .GreaterThan(0)
            .When(x => x.StatusId.HasValue);

        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0)
            .When(x => x.AssignedUserId.HasValue);

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OrderIndex.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue);
    }
}

public class RequestDeleteTaskValidator : AbstractValidator<DeleteTaskRequest>
{
    public RequestDeleteTaskValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0);
    }
}

public class RequestGetTasksValidator : AbstractValidator<GetTasksRequest>
{
    public RequestGetTasksValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .When(x => x.BoardId.HasValue);

        RuleFor(x => x.ListId)
            .GreaterThan(0)
            .When(x => x.ListId.HasValue);

        RuleFor(x => x.StatusId)
            .GreaterThan(0)
            .When(x => x.StatusId.HasValue);

        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0)
            .When(x => x.AssignedUserId.HasValue);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
