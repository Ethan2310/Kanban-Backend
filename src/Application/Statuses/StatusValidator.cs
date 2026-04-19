using FluentValidation;

namespace Application.Statuses;

public class CreateStatusRequestValidator : AbstractValidator<CreateStatusRequest>
{
    public CreateStatusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Length(7)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid hex color in the format #RRGGBB.");
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Length(7)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid hex color in the format #RRGGBB.");
    }
}

public class DeleteStatusRequestValidator : AbstractValidator<DeleteStatusRequest>
{
    public DeleteStatusRequestValidator()
    {
        RuleFor(x => x.StatusId)
            .GreaterThan(0);
    }
}

public class GetStatusesRequestValidator : AbstractValidator<GetStatusesRequest>
{
    public GetStatusesRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
