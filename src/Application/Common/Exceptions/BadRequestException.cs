namespace Application.Common.Exceptions;

public class BadRequestException : Exception, IAppException
{
    public int StatusCode => 400;
    public string ErrorCode => "BAD_REQUEST";

    public BadRequestException(string message) : base(message) { }
}
