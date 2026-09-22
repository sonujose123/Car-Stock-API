using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

public sealed class GetCarEndpoint(CarRepository cars) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/cars/{id}");
        Policies("Dealer");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!CarRequest.TryCarId(HttpContext, out var id))
        {
            await Send.ResultAsync(CarRequest.InvalidId(HttpContext));
            return;
        }
        var car = await cars.GetAsync(CarRequest.Id(User), id, ct);
        await Send.ResultAsync(car is null ? CarRequest.NotFound(HttpContext) : Results.Ok(car));
    }
}



