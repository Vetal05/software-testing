namespace NewsAggregator.Core.Articles;

/// <summary>
/// Feed visibility: articles from inactive sources must not appear in the public list.
/// </summary>
public static class ActiveSourceArticleVisibility
{
    public static IEnumerable<Guid> VisibleArticleIds(
        IEnumerable<(Guid ArticleId, bool SourceIsActive)> rows) =>
        rows.Where(r => r.SourceIsActive).Select(r => r.ArticleId);
}
