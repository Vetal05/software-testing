using System.ComponentModel.DataAnnotations;
using NewsAggregator.Core;

namespace NewsAggregator.Api.Models.Dtos;

public record SourceResponse(Guid Id, string Name, string Url, NewsCategory Category, bool IsActive);

public record CreateSourceRequest(
    [Required] [MaxLength(500)] string Name,
    [Required] [MaxLength(2000)] string Url,
    [Required] NewsCategory Category,
    bool IsActive = true);
