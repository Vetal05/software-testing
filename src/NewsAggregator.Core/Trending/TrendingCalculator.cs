namespace NewsAggregator.Core.Trending;

/// <summary>
/// Pure ranking logic: bookmark counts within a sliding UTC window (e.g. last 7 days).
/// </summary>
public static class TrendingCalculator
{
    public static IReadOnlyList<Guid> RankArticleIdsByBookmarksInWindow(
        IEnumerable<(Guid ArticleId, DateTime CreatedAtUtc)> bookmarks,
        DateTime utcNow,
        TimeSpan window,
        int take)
    {
        if (take < 1)
            return Array.Empty<Guid>();

        var threshold = utcNow - window;
        return bookmarks
            .Where(b => b.CreatedAtUtc >= threshold)
            .GroupBy(b => b.ArticleId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .Take(take)
            .ToList();
    }
}
