using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace NewsAggregator.Tests.Database;

public class PostgresSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
