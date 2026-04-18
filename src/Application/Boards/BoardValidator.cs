using FluentValidation;

namespace Application.Boards;

public class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

public class UpdateBoardRequestValidator : AbstractValidator<UpdateBoardRequest>
{
    public UpdateBoardRequestValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

public class DeleteBoardRequestValidator : AbstractValidator<DeleteBoardRequest>
{
    public DeleteBoardRequestValidator()
    {
        RuleFor(x => x.BoardID)
            .GreaterThan(0);
    }
}

public class GetBoardsRequestValidator : AbstractValidator<GetBoardsRequest>
{
    public GetBoardsRequestValidator()
    {
        RuleFor(x => x.ProjectId)
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
