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
            .MaximumLength(1000);

        RuleFor(x => x.BoardId)
            .GreaterThan(0);

        RuleFor(x => x.ListId)
            .GreaterThan(0);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);

        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0);
    }
}
