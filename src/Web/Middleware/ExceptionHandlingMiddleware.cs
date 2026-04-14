using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;

using Application.Common.Exceptions;

namespace Web.Middleware;
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        if (exception is IAppException appEx)
        {
            context.Response.StatusCode = appEx.StatusCode;

            var body = new ErrorResponse(
                Status: appEx.StatusCode,
                Title: ReasonPhraseFor(appEx.StatusCode),
                Detail: exception.Message,
                ErrorCode: appEx.ErrorCode,
                Errors: exception is ValidationException ve ? ve.Errors : null
            );

            await context.Response.WriteAsJsonAsync(body, JsonOptions);
        }
        else
        {
            // Fallback: unhandled / unexpected exception → 500.
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 500;

            var body = new ErrorResponse(
                Status: 500,
                Title: "Internal Server Error",
                Detail: "An unexpected error occurred.",
                ErrorCode: "INTERNAL_ERROR",
                Errors: null
            );

            await context.Response.WriteAsJsonAsync(body, JsonOptions);
        }
    }

    private static string ReasonPhraseFor(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        _ => "Error"
    };

    private record ErrorResponse(
        int Status,
        string Title,
        string Detail,
        string ErrorCode,
        IDictionary<string, string[]>? Errors
    );
}
