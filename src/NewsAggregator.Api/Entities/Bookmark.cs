namespace NewsAggregator.Api.Entities;

public class Bookmark
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}
