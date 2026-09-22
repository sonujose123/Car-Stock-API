using System.Text.Json;
using CarStock.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
foreach (var (exception, expected) in new (Exception, int)[]
{
    (new InvalidOperationException("PRIVATE database path and secret"), 500),
    (new JsonException("PRIVATE parser detail"), 400),
    (new BadHttpRequestException("PRIVATE request detail", 413), 413)
})
{
    var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "test-trace" };
    context.Response.Body = new MemoryStream();
    var middleware = new ApiErrorMiddleware(_ => throw exception, NullLogger<ApiErrorMiddleware>.Instance);
    await middleware.InvokeAsync(context);
    context.Response.Body.Position = 0;
    var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
    using var json = JsonDocument.Parse(body);
    if (context.Response.StatusCode != expected || json.RootElement.GetProperty("status").GetInt32() != expected
        || body.Contains("PRIVATE") || json.RootElement.GetProperty("traceId").GetString() != "test-trace"
        || json.RootElement.GetProperty("errors").ValueKind != JsonValueKind.Object)
        throw new Exception($"Unsafe or inconsistent response: {body}");
    Console.WriteLine($"PASS: exception returns safe JSON {expected}");
}
