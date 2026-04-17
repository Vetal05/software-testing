using NewsAggregator.Core.Articles;

namespace NewsAggregator.Tests.Unit;

public class ActiveSourceArticleVisibilityTests
{
    [Fact]
    public void Hides_articles_from_inactive_sources()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var rows = new[]
        {
            (a, SourceIsActive: true),
            (b, SourceIsActive: false)
        };

        var visible = ActiveSourceArticleVisibility.VisibleArticleIds(rows).ToList();

        Assert.Single(visible);
        Assert.Equal(a, visible[0]);
    }
}
