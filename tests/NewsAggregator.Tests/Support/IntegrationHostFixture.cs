using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Api;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Data.Seeding;
using Testcontainers.PostgreSql;
using Xunit;

namespace NewsAggregator.Tests.Support;

public class IntegrationHostFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var cs = _postgres.GetConnectionString();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", cs);
            builder.ConfigureTestServices(services =>
            {
                foreach (var d in services.Where(x =>
                             x.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                             x.ServiceType == typeof(AppDbContext)).ToList())
                    services.Remove(d);

                services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));
            });
        });

        _ = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await BulkDataSeeder.SeedAsync(db);

        var total = await db.Sources.CountAsync() + await db.Articles.CountAsync() + await db.Bookmarks.CountAsync();
        if (total < 10_000)
            throw new InvalidOperationException($"Expected at least 10_000 seeded rows, got {total}.");
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
