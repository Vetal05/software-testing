namespace NewsAggregator.Core.Articles;

/// <summary>
/// URL comparison for duplicate detection (normalization rules used by the API layer).
/// </summary>
public static class ArticleUrlDuplicateDetector
{
    public static string Normalize(string url)
    {
        if (url is null)
            throw new ArgumentNullException(nameof(url));

        var trimmed = url.Trim();
        return trimmed.TrimEnd('/').ToLowerInvariant();
    }

    public static bool AreDuplicates(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);
}
