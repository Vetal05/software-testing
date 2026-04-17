using NewsAggregator.Core;

namespace NewsAggregator.Api.Entities;

public class Article
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public Source Source { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public string? ImageUrl { get; set; }
    public NewsCategory Category { get; set; }

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
