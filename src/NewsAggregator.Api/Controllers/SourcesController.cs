using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Entities;
using NewsAggregator.Api.Models.Dtos;
using NewsAggregator.Core.Articles;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SourceResponse>>> List(CancellationToken ct = default)
    {
        var items = await db.Sources.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SourceResponse(s.Id, s.Name, s.Url, s.Category, s.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<SourceResponse>> Create(
        [FromBody] CreateSourceRequest request,
        CancellationToken ct = default)
    {
        var canonicalUrl = ArticleUrlDuplicateDetector.Normalize(request.Url);
        if (await db.Sources.AnyAsync(s => s.Url == canonicalUrl, ct))
            return Conflict(new { message = "Source URL must be unique." });

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = canonicalUrl,
            Category = request.Category,
            IsActive = request.IsActive
        };

        db.Sources.Add(source);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(List), new { id = source.Id },
            new SourceResponse(source.Id, source.Name, source.Url, source.Category, source.IsActive));
    }
}
