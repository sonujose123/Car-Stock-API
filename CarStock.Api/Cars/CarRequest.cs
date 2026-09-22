using System.Globalization;
using System.Security.Claims;
using CarStock.Api.Auth;
using CarStock.Api.Errors;
using FastEndpoints;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Cars;

internal static class CarRequest
{
    public static long Id(ClaimsPrincipal user) => long.Parse(
        user.FindFirst(TokenSettings.DealerIdClaim)!.Value, CultureInfo.InvariantCulture);

    public static IResult NotFound(HttpContext context) => ApiError.Result(context, 404, "Car not found.");

    public static bool TryCarId(HttpContext context, out long id) =>
        long.TryParse(context.Request.RouteValues["id"]?.ToString(), NumberStyles.None,
            CultureInfo.InvariantCulture, out id) && id > 0;

    public static IResult InvalidId(HttpContext context) => ApiError.Result(context, 400,
        "One or more validation errors occurred.", new() { ["id"] = ["Car ID must be a positive integer."] });
}


