using NewsAggregator.Core;

namespace NewsAggregator.Api.Entities;

public class Source
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public NewsCategory Category { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
