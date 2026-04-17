using Bogus;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Entities;
using NewsAggregator.Core;

namespace NewsAggregator.Api.Data.Seeding;

public static class BulkDataSeeder
{
    /// <summary>
    /// Seeds at least <paramref name="totalRows"/> rows across Sources, Articles, and Bookmarks
    /// with realistic parent/child ratios.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, int totalRows = 10_000, CancellationToken ct = default)
    {
        if (await db.Sources.AnyAsync(ct))
            return;

        var sourceCount = Math.Max(50, totalRows / 100);
        var bookmarkCount = Math.Max(100, totalRows / 50);
        var articleCount = Math.Max(totalRows - sourceCount - bookmarkCount, totalRows * 9 / 10);

        var categories = new[] { NewsCategory.Tech, NewsCategory.Sports, NewsCategory.Politics, NewsCategory.Science, NewsCategory.Business };

        var sourceFaker = new Faker<Source>()
            .RuleFor(s => s.Id, _ => Guid.NewGuid())
            .RuleFor(s => s.Name, f => f.Company.CompanyName())
            .RuleFor(s => s.Url, f => f.Internet.UrlWithPath() + "/" + Guid.NewGuid().ToString("N")[..8])
            .RuleFor(s => s.Category, f => f.PickRandom(categories))
            .RuleFor(s => s.IsActive, f => f.Random.Bool(0.92f));

        var sources = sourceFaker.Generate(sourceCount);
        await db.Sources.AddRangeAsync(sources, ct);
        await db.SaveChangesAsync(ct);

        var sourceIds = sources.Select(s => s.Id).ToList();
        var articleFaker = new Faker<Article>()
            .RuleFor(a => a.Id, _ => Guid.NewGuid())
            .RuleFor(a => a.SourceId, f => f.PickRandom(sourceIds))
            .RuleFor(a => a.Title, f => f.Lorem.Sentence(3, 8))
            .RuleFor(a => a.Summary, f => f.Lorem.Paragraph())
            .RuleFor(a => a.Url, f => f.Internet.UrlWithPath() + "/a/" + Guid.NewGuid().ToString("N"))
            .RuleFor(a => a.PublishedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-365), DateTime.UtcNow))
            .RuleFor(a => a.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(a => a.Category, f => f.PickRandom(categories));

        const int batch = 500;
        for (var i = 0; i < articleCount; i += batch)
        {
            var take = Math.Min(batch, articleCount - i);
            var batchArticles = articleFaker.Generate(take);
            await db.Articles.AddRangeAsync(batchArticles, ct);
            await db.SaveChangesAsync(ct);
        }

        var articleIds = await db.Articles.AsNoTracking().Select(a => a.Id).ToListAsync(ct);
        var userIds = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();

        var bookmarkFaker = new Faker<Bookmark>()
            .RuleFor(b => b.Id, _ => Guid.NewGuid())
            .RuleFor(b => b.UserId, f => f.PickRandom(userIds))
            .RuleFor(b => b.ArticleId, f => f.PickRandom(articleIds))
            .RuleFor(b => b.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow))
            .RuleFor(b => b.Notes, f => f.Random.Bool(0.3f) ? f.Lorem.Sentence() : null);

        var bookmarks = new List<Bookmark>();
        var pairs = new HashSet<(Guid UserId, Guid ArticleId)>();
        while (bookmarks.Count < bookmarkCount)
        {
            var b = bookmarkFaker.Generate();
            if (pairs.Add((b.UserId, b.ArticleId)))
                bookmarks.Add(b);
        }

        await db.Bookmarks.AddRangeAsync(bookmarks, ct);
        await db.SaveChangesAsync(ct);
    }
}
