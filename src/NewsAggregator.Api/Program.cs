using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "News Aggregator API",
        Version = "v1",
        Description = "Агрегатор новин (завдання 26). У групі **Dev — запуск тестів** можна прогнати xUnit з браузера (лише Development)."
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=news_aggregator;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.DisplayRequestDuration();
        o.DefaultModelsExpandDepth(5);
        o.DefaultModelExpandDepth(4);
        o.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        o.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var server = app.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
    if (addresses is null)
        return;
    foreach (var address in addresses)
    {
        var root = address.TrimEnd('/');
        app.Logger.LogInformation("Swagger UI: {SwaggerUrl}", $"{root}/swagger");
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("NewsAggregator:SeedOnStartup", false))
        await BulkDataSeeder.SeedAsync(db);
}

await app.RunAsync();
