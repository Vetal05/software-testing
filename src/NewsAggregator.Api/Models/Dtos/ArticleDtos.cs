using System.ComponentModel.DataAnnotations;
using NewsAggregator.Core;

namespace NewsAggregator.Api.Models.Dtos;

public record ArticleResponse(
    Guid Id,
    Guid SourceId,
    string Title,
    string Summary,
    string Url,
    DateTime PublishedAt,
    string? ImageUrl,
    NewsCategory Category);

public record PagedArticlesResponse(
    IReadOnlyList<ArticleResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record CreateArticleRequest(
    [Required] Guid SourceId,
    [Required] [MaxLength(500)] string Title,
    [Required] [MaxLength(8000)] string Summary,
    [Required] [MaxLength(2000)] string Url,
    DateTime PublishedAt,
    [MaxLength(2000)] string? ImageUrl,
    [Required] NewsCategory Category);

public record TrendingArticleResponse(Guid ArticleId, int BookmarksInWindow);
