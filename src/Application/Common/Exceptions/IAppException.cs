namespace Application.Common.Exceptions;
public interface IAppException
{
    int StatusCode { get; }
    string ErrorCode { get; }
}
