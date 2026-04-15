namespace Application.Common.Exceptions;

public class UnauthorizedException : Exception, IAppException
{
    public int StatusCode => 401;
    public string ErrorCode => "UNAUTHORIZED";

    public UnauthorizedException(string message) : base(message) { }
}
