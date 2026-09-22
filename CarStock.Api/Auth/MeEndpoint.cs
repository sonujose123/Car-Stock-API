using FastEndpoints;

namespace CarStock.Api.Auth;

public sealed class MeEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/auth/me");
        Policies("Dealer");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new { DealerId = long.Parse(User.FindFirst(TokenSettings.DealerIdClaim)!.Value) }, ct);
    }
}
