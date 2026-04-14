using FluentValidation.Results;

namespace Application.Common.Exceptions;

public class ValidationException : Exception, IAppException
{
    public int StatusCode => 400;
    public string ErrorCode => "VALIDATION_FAILED";
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures have occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
