using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

public sealed class ListCarsEndpoint(CarRepository cars) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/cars");
        Policies("Dealer");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var make = Query<string>("make", isRequired: false)?.Trim();
        var model = Query<string>("model", isRequired: false)?.Trim();
        if (make?.Length > 100 || model?.Length > 100)
        {
            var errors = new Dictionary<string, string[]>();
            if (make?.Length > 100) errors["make"] = ["Make must be at most 100 characters."];
            if (model?.Length > 100) errors["model"] = ["Model must be at most 100 characters."];
            await Send.ResultAsync(ApiError.Result(HttpContext, 400, "One or more validation errors occurred.", errors));
            return;
        }
        var result = await cars.ListAsync(CarRequest.Id(User),
            string.IsNullOrEmpty(make) ? null : make,
            string.IsNullOrEmpty(model) ? null : model, ct);
        await Send.OkAsync(result, ct);
    }
}



