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
