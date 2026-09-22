namespace CarStock.Api.Auth;

public sealed record TokenSettings(string SigningKey)
{
    public const string Issuer = "CarStock.Api";
    public const string Audience = "CarStock.Dealers";
    public const string DealerIdClaim = "dealer_id";
}
