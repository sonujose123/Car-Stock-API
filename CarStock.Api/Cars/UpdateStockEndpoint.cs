using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

public sealed class UpdateStockEndpoint(CarRepository cars) : Endpoint<UpdateStockRequest>
{
    public override void Configure()
    {
        Put("/api/cars/{id}/stock");
        Policies("Dealer");
    }

    public override async Task HandleAsync(UpdateStockRequest req, CancellationToken ct)
    {
        if (!CarRequest.TryCarId(HttpContext, out var id))
        {
            await Send.ResultAsync(CarRequest.InvalidId(HttpContext));
            return;
        }
        var car = await cars.UpdateStockAsync(CarRequest.Id(User), id, req.StockLevel!.Value, ct);
        await Send.ResultAsync(car is null ? CarRequest.NotFound(HttpContext) : Results.Ok(car));
    }
}



