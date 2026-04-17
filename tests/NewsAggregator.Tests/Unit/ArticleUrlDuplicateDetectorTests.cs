using NewsAggregator.Core.Articles;

namespace NewsAggregator.Tests.Unit;

public class ArticleUrlDuplicateDetectorTests
{
    [Theory]
    [InlineData("https://example.com/a", "HTTPS://example.com/a/")]
    [InlineData(" https://example.com/x ", "https://example.com/x")]
    public void Detects_duplicates_after_normalization(string left, string right) =>
        Assert.True(ArticleUrlDuplicateDetector.AreDuplicates(left, right));

    [Fact]
    public void Distinct_urls_are_not_duplicates() =>
        Assert.False(ArticleUrlDuplicateDetector.AreDuplicates("https://a.test/1", "https://b.test/1"));
}
