using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Entities;
using NewsAggregator.Core;
using NewsAggregator.Core.Articles;
using Xunit;

namespace NewsAggregator.Tests.Database;

public class EfDatabaseTests : IClassFixture<PostgresSqlFixture>
{
    private readonly PostgresSqlFixture _fx;

    public EfDatabaseTests(PostgresSqlFixture fx) => _fx = fx;

    [Fact]
    public async Task Source_url_uniqueness_enforced_at_database()
    {
        await using var db = _fx.CreateContext();
        var url = ArticleUrlDuplicateDetector.Normalize($"https://src-dup-{Guid.NewGuid():N}.test/");
        db.Sources.Add(new Source
        {
            Id = Guid.NewGuid(),
            Name = "a",
            Url = url,
            Category = NewsCategory.Tech,
            IsActive = true
        });
        await db.SaveChangesAsync();

        db.Sources.Add(new Source
        {
            Id = Guid.NewGuid(),
            Name = "b",
            Url = url,
            Category = NewsCategory.Tech,
            IsActive = true
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Article_url_uniqueness_enforced_at_database()
    {
        await using var db = _fx.CreateContext();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "s",
            Url = $"https://src-{Guid.NewGuid():N}.test/",
            Category = NewsCategory.Tech,
            IsActive = true
        };
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var url = ArticleUrlDuplicateDetector.Normalize("https://dup.test/article");
        db.Articles.Add(new Article
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            Title = "t1",
            Summary = "s",
            Url = url,
            PublishedAt = DateTime.UtcNow,
            Category = NewsCategory.Tech
        });
        await db.SaveChangesAsync();

        db.Articles.Add(new Article
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            Title = "t2",
            Summary = "s",
            Url = url,
            PublishedAt = DateTime.UtcNow,
            Category = NewsCategory.Tech
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Trending_query_respects_7_day_window_in_sql()
    {
        await using var db = _fx.CreateContext();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "s2",
            Url = $"https://src-{Guid.NewGuid():N}.test/",
            Category = NewsCategory.Science,
            IsActive = true
        };
        db.Sources.Add(source);
        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            Title = "t",
            Summary = "s",
            Url = $"https://art-{Guid.NewGuid():N}.test/",
            PublishedAt = DateTime.UtcNow,
            Category = NewsCategory.Science
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var user = Guid.NewGuid();
        db.Bookmarks.Add(new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = user,
            ArticleId = article.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Notes = null
        });
        db.Bookmarks.Add(new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ArticleId = article.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            Notes = null
        });
        await db.SaveChangesAsync();

        var windowStart = DateTime.UtcNow.AddDays(-7);
        var countInWindow = await (
            from b in db.Bookmarks.AsNoTracking()
            where b.CreatedAt >= windowStart && b.ArticleId == article.Id
            select b).CountAsync();

        Assert.Equal(1, countInWindow);
    }

    [Fact]
    public async Task Deleting_source_cascades_to_articles()
    {
        await using var db = _fx.CreateContext();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "s3",
            Url = $"https://src-{Guid.NewGuid():N}.test/",
            Category = NewsCategory.Business,
            IsActive = true
        };
        db.Sources.Add(source);
        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            Title = "t",
            Summary = "s",
            Url = $"https://art-{Guid.NewGuid():N}.test/",
            PublishedAt = DateTime.UtcNow,
            Category = NewsCategory.Business
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        db.Sources.Remove(source);
        await db.SaveChangesAsync();

        var exists = await db.Articles.AnyAsync(a => a.Id == article.Id);
        Assert.False(exists);
    }
}
