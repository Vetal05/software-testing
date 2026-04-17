using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewsAggregator.Api.Models.Dtos;
using NewsAggregator.Core;
using NewsAggregator.Tests.Support;
using Xunit;

namespace NewsAggregator.Tests.Integration;

[Collection("Integration")]
public class ArticlesFeedIntegrationTests(IntegrationHostFixture host)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = host.Factory.CreateClient();

    [Fact]
    public async Task Articles_list_supports_pagination_and_filters()
    {
        var first = await _client.GetFromJsonAsync<PagedArticlesResponse>(
            "/api/articles?page=1&pageSize=5", Json);
        Assert.NotNull(first);
        Assert.NotEmpty(first!.Items);
        Assert.True(first.TotalCount >= first.Items.Count);
        Assert.Equal(1, first.Page);
        Assert.Equal(5, first.PageSize);

        var tech = await _client.GetFromJsonAsync<PagedArticlesResponse>(
            $"/api/articles?category={NewsCategory.Tech}&pageSize=10", Json);
        Assert.NotNull(tech);
        Assert.All(tech!.Items, a => Assert.Equal(NewsCategory.Tech, a.Category));
    }

    [Fact]
    public async Task Article_details_available_even_when_source_inactive_not_required_for_list()
    {
        var page = await _client.GetFromJsonAsync<PagedArticlesResponse>("/api/articles?pageSize=1", Json);
        Assert.NotNull(page);
        var id = page!.Items[0].Id;

        var res = await _client.GetAsync($"/api/articles/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
