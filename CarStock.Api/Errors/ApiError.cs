using System.Text.Json;
using FluentValidation.Results;

namespace CarStock.Api.Errors;

public sealed record ApiError(int Status, string Message, Dictionary<string, string[]> Errors, string TraceId)
{
    public static ApiError Create(HttpContext context, int status, string? message = null,
        Dictionary<string, string[]>? errors = null) => new(status, message ?? status switch
        {
            400 => "The request is invalid.",
            401 => "Authentication is required or the supplied credentials are invalid.",
            403 => "You do not have permission to perform this action.",
            404 => "The requested resource was not found.",
            405 => "This HTTP method is not allowed for this resource.",
            415 => "Content-Type must be application/json.",
            500 => "An unexpected error occurred. Please try again later.",
            _ => "The request could not be completed."
        }, errors ?? new(), context.TraceIdentifier);

    public static ApiError Validation(HttpContext context, int status, IEnumerable<ValidationFailure> failures)
    {
        var errors = new Dictionary<string, List<string>>();
        foreach (var failure in failures)
        {
            var field = JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName);
            var message = failure.PropertyName.StartsWith('$')
                ? "Provide valid JSON with the expected field types."
                : failure.ErrorMessage;

            if (!errors.ContainsKey(field))
                errors[field] = new List<string>();

            if (!errors[field].Contains(message))
                errors[field].Add(message);
        }

        return Create(context, status, "One or more validation errors occurred.",
            errors.ToDictionary(item => item.Key, item => item.Value.ToArray()));
    }
    public static IResult Result(HttpContext context, int status, string? message = null,
        Dictionary<string, string[]>? errors = null) => Results.Json(Create(context, status, message, errors), statusCode: status);
}

