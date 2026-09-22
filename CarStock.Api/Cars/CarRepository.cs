using CarStock.Api.Data;
using Dapper;

namespace CarStock.Api.Cars;

public sealed class CarRepository(Database database)
{
    public async Task<Car> AddAsync(long dealerId, AddCarRequest request, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        const string sql = """
            INSERT INTO Cars (DealerId, Make, Model, Year, StockLevel)
            VALUES (@DealerId, @Make, @Model, @Year, @StockLevel)
            RETURNING Id, Make, Model, Year, StockLevel;
            """;

        var parameters = new
        {
            DealerId = dealerId,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            request.Year,
            request.StockLevel
        };
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        return await connection.QuerySingleAsync<Car>(command);
    }

    public async Task<Car?> GetAsync(long dealerId, long id, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        const string sql = """
            SELECT Id, Make, Model, Year, StockLevel FROM Cars
            WHERE DealerId = @DealerId AND Id = @Id;
            """;

        var parameters = new { DealerId = dealerId, Id = id };
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Car>(command);
    }

    public async Task<IEnumerable<Car>> ListAsync(long dealerId, string? make, string? model, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        // instr treats %, _ and quotes as literal characters, not wildcard SQL.
        const string sql = """
            SELECT Id, Make, Model, Year, StockLevel FROM Cars
            WHERE DealerId = @DealerId
              AND (@Make IS NULL OR instr(lower(Make), lower(@Make)) > 0)
              AND (@Model IS NULL OR instr(lower(Model), lower(@Model)) > 0)
            ORDER BY Make, Model, Year, Id;
            """;

        var parameters = new { DealerId = dealerId, Make = make, Model = model };
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        return await connection.QueryAsync<Car>(command);
    }

    public async Task<Car?> UpdateStockAsync(long dealerId, long id, int stockLevel, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        // The ownership check and write happen together in one atomic statement.
        const string sql = """
            UPDATE Cars SET StockLevel = @StockLevel
            WHERE DealerId = @DealerId AND Id = @Id
            RETURNING Id, Make, Model, Year, StockLevel;
            """;

        var parameters = new { DealerId = dealerId, Id = id, StockLevel = stockLevel };
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Car>(command);
    }

    public async Task<bool> DeleteAsync(long dealerId, long id, CancellationToken ct)
    {
        await using var connection = database.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM Cars WHERE DealerId = @DealerId AND Id = @Id;
            """, new { DealerId = dealerId, Id = id }, cancellationToken: ct)) > 0;
    }
}


