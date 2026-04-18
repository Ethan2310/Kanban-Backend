using FluentValidation;

namespace Application.Projects;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

public class DeleteProjectRequestValidator : AbstractValidator<DeleteProjectRequest>
{
    public DeleteProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);
    }
}

public class GetProjectsRequestValidator : AbstractValidator<GetProjectsRequest>
{
    public GetProjectsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.BoardId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .MaximumLength(150);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
