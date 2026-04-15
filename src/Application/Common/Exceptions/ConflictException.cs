namespace Application.Common.Exceptions;

public class ConflictException : Exception, IAppException
{
    public int StatusCode => 409;
    public string ErrorCode => "CONFLICT";

    public ConflictException(string message) : base(message) { }
}
