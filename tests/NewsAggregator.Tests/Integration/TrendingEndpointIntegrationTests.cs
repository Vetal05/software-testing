using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewsAggregator.Api.Models.Dtos;
using NewsAggregator.Tests.Support;
using Xunit;

namespace NewsAggregator.Tests.Integration;

[Collection("Integration")]
public class TrendingEndpointIntegrationTests(IntegrationHostFixture host)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = host.Factory.CreateClient();

    [Fact]
    public async Task Trending_returns_ordered_results()
    {
        var trends = await _client.GetFromJsonAsync<List<TrendingArticleResponse>>(
            "/api/articles/trending?limit=5", Json);

        Assert.NotNull(trends);
        for (var i = 1; i < trends!.Count; i++)
            Assert.True(trends[i - 1].BookmarksInWindow >= trends[i].BookmarksInWindow);
    }
}
