using FluentValidation;

namespace Application.Lists;

public class CreateListRequestValidator : AbstractValidator<CreateListRequest>
{
    public CreateListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.BoardId)
            .GreaterThan(0);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);
    }
}

public class UpdateListRequestValidator : AbstractValidator<UpdateListRequest>
{
    public UpdateListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .When(x => x.Name != null);

        RuleFor(x => x.StatusId)
            .GreaterThan(0)
            .When(x => x.StatusId.HasValue);
    }
}

public class DeleteListRequestValidator : AbstractValidator<DeleteListRequest>
{
    public DeleteListRequestValidator()
    {
        RuleFor(x => x.ListId)
            .GreaterThan(0);
    }
}

public class GetListsRequestValidator : AbstractValidator<GetListsRequest>
{
    public GetListsRequestValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .When(x => x.BoardId.HasValue);

        RuleFor(x => x.Name)
            .MaximumLength(150)
            .When(x => x.Name != null);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
