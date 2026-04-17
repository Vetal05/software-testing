using NewsAggregator.Core.Trending;

namespace NewsAggregator.Tests.Unit;

public class TrendingCalculatorTests
{
    [Fact]
    public void Ranks_by_bookmark_count_within_window_and_excludes_older()
    {
        var now = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var articleA = Guid.NewGuid();
        var articleB = Guid.NewGuid();
        var articleC = Guid.NewGuid();

        var bookmarks = new (Guid ArticleId, DateTime CreatedAtUtc)[]
        {
            (articleA, now.AddDays(-1)),
            (articleA, now.AddDays(-2)),
            (articleB, now.AddDays(-3)),
            (articleC, now.AddDays(-10))
        };

        var ranked = TrendingCalculator.RankArticleIdsByBookmarksInWindow(
            bookmarks,
            now,
            TimeSpan.FromDays(7),
            take: 10);

        Assert.Equal(new[] { articleA, articleB }, ranked);
    }

    [Fact]
    public void Take_limits_results()
    {
        var now = DateTime.UtcNow;
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var bookmarks = new[]
        {
            (a, now), (a, now), (b, now), (c, now)
        };

        var ranked = TrendingCalculator.RankArticleIdsByBookmarksInWindow(bookmarks, now, TimeSpan.FromDays(7), take: 2);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(a, ranked[0]);
    }
}
