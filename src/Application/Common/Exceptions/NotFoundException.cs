namespace Application.Common.Exceptions;
public class NotFoundException : Exception, IAppException
{
    public int StatusCode => 404;
    public string ErrorCode => "NOT_FOUND";

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with id '{key}' was not found.") { }
}
