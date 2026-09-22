using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

public sealed class DeleteCarEndpoint(CarRepository cars) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/cars/{id}");
        Policies("Dealer");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!CarRequest.TryCarId(HttpContext, out var id))
        {
            await Send.ResultAsync(CarRequest.InvalidId(HttpContext));
            return;
        }
        var deleted = await cars.DeleteAsync(CarRequest.Id(User), id, ct);
        await Send.ResultAsync(deleted ? Results.Ok(new { message = "Car deleted." }) : CarRequest.NotFound(HttpContext));
    }
}


