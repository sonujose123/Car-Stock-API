using CarStock.Api.Data;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace CarStock.Api.Auth;

public static class DevelopmentDealers
{
    public static async Task SeedAsync(Database database, IPasswordHasher<Dealer> hasher)
    {
        await using var connection = database.CreateConnection();
        foreach (var email in new[] { "dealer1@example.com", "dealer2@example.com" })
        {
            var dealer = new Dealer { Name = email.Split('@')[0], Email = email };
            dealer.PasswordHash = hasher.HashPassword(dealer, "DemoPassword123!");
            await connection.ExecuteAsync("""
                INSERT INTO Dealers (Name, Email, PasswordHash)
                VALUES (@Name, @Email, @PasswordHash)
                ON CONFLICT (Email) DO NOTHING;
                """, dealer);
        }
    }
}
