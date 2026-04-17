using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Api.Models.Dtos;

public record BookmarkResponse(
    Guid Id,
    Guid UserId,
    Guid ArticleId,
    DateTime CreatedAt,
    string? Notes);

public record CreateBookmarkRequest(
    [Required] Guid UserId,
    [Required] Guid ArticleId,
    [MaxLength(4000)] string? Notes,
    DateTime? CreatedAt);
