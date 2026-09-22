using System.Text.Json;

namespace CarStock.Api.Errors;

public sealed class ApiErrorMiddleware(RequestDelegate next, ILogger<ApiErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected; do not attempt to write a response.
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var status = exception switch
            {
                JsonException => 400,
                BadHttpRequestException badRequest => badRequest.StatusCode,
                _ => 500
            };
            if (status >= 500)
                logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);

            context.Response.Clear();
            await ApiError.Result(context, status).ExecuteAsync(context);
        }
    }
}
