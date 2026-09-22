using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

public sealed class AddCarEndpoint(CarRepository cars) : Endpoint<AddCarRequest>
{
    public override void Configure()
    {
        Post("/api/cars");
        Policies("Dealer");
    }

    public override async Task HandleAsync(AddCarRequest req, CancellationToken ct)
    {
        try
        {
            var car = await cars.AddAsync(CarRequest.Id(User), req, ct);
            await Send.ResultAsync(Results.Created($"/api/cars/{car.Id}", car));
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            await Send.ResultAsync(ApiError.Result(HttpContext, 409,
                "This make, model, and year already exists in your stock. Update its stock level instead."));
        }
    }
}



