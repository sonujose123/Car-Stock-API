using System.Globalization;
using CarStock.Api.Data;
using CarStock.Api.Errors;
using Dapper;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CarStock.Api.Auth;

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);

public sealed class LoginEndpoint(Database database, IPasswordHasher<Dealer> hasher, TokenSettings settings)
    : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        var dealer = await connection.QuerySingleOrDefaultAsync<Dealer>(new CommandDefinition(
            "SELECT Id, Name, Email, PasswordHash FROM Dealers WHERE Email = @Email;",
            new { Email = req.Email.Trim() }, cancellationToken: ct));

        if (dealer is null)
        {
            await Send.ResultAsync(ApiError.Result(HttpContext, 401, "Invalid email or password."));
            return;
        }

        var result = hasher.VerifyHashedPassword(dealer, dealer.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await Send.ResultAsync(ApiError.Result(HttpContext, 401, "Invalid email or password."));
            return;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Dealers SET PasswordHash = @Hash WHERE Id = @Id;",
                new { Hash = hasher.HashPassword(dealer, req.Password), dealer.Id }, cancellationToken: ct));
        }

        var expires = DateTime.UtcNow.AddMinutes(30);
        var token = JwtBearer.CreateToken(options =>
        {
            options.SigningKey = settings.SigningKey;
            options.Issuer = TokenSettings.Issuer;
            options.Audience = TokenSettings.Audience;
            options.ExpireAt = expires;
            var id = dealer.Id.ToString(CultureInfo.InvariantCulture);
            options.User.Claims.Add((TokenSettings.DealerIdClaim, id), ("sub", id));
        });
        await Send.OkAsync(new(token, "Bearer", expires), ct);
    }
}
