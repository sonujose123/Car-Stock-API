using Dapper;
using Microsoft.Data.Sqlite;

namespace CarStock.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(Database database)
    {
        await using var connection = database.CreateConnection();
        await connection.OpenAsync();

        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql"));
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(sql, transaction: transaction);
        transaction.Commit();
    }
}
