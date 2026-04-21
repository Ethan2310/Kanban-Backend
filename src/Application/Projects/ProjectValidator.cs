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
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null);
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

public class AddUserToProjectRequestValidator : AbstractValidator<AddUserToProjectRequest>
{
    public AddUserToProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.UserId)
            .GreaterThan(0);
    }
}

public class GetUsersInProjectRequestValidator : AbstractValidator<GetUsersInProjectRequest>
{
    public GetUsersInProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}

public class RemoveUserFromProjectRequestValidator : AbstractValidator<RemoveUserFromProjectRequest>
{
    public RemoveUserFromProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.UserId)
            .GreaterThan(0);
    }
}
