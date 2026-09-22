using Microsoft.Data.Sqlite;

namespace CarStock.Api.Data;

public sealed class Database(string path)
{
    public SqliteConnection CreateConnection() => new(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        ForeignKeys = true
    }.ToString());
}
