using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Models.Dtos;
using NewsAggregator.Core;
using NewsAggregator.Tests.Support;
using Xunit;

namespace NewsAggregator.Tests.Integration;

[Collection("Integration")]
public class BookmarksFlowIntegrationTests(IntegrationHostFixture host)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = host.Factory.CreateClient();

    [Fact]
    public async Task Bookmark_create_list_delete_flow()
    {
        var userId = Guid.NewGuid();

        Guid articleId;
        await using (var scope = host.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            articleId = await db.Articles.AsNoTracking()
                .Where(a => a.Source.IsActive)
                .Select(a => a.Id)
                .FirstAsync();
        }

        var create = new CreateBookmarkRequest(userId, articleId, Notes: "integration", CreatedAt: null);
        var post = await _client.PostAsJsonAsync("/api/bookmarks", create, Json);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<BookmarkResponse>(Json);
        Assert.NotNull(created);

        var list = await _client.GetFromJsonAsync<List<BookmarkResponse>>(
            $"/api/bookmarks?userId={userId}", Json);
        Assert.NotNull(list);
        Assert.Contains(list!, b => b.ArticleId == articleId);

        var dup = await _client.PostAsJsonAsync("/api/bookmarks", create, Json);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var del = await _client.DeleteAsync($"/api/bookmarks/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var after = await _client.GetFromJsonAsync<List<BookmarkResponse>>(
            $"/api/bookmarks?userId={userId}", Json);
        Assert.DoesNotContain(after!, b => b.Id == created.Id);
    }
}
