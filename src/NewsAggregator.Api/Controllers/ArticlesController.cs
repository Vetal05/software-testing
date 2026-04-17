using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Entities;
using NewsAggregator.Api.Models.Dtos;
using NewsAggregator.Core;
using NewsAggregator.Core.Articles;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedArticlesResponse>> List(
        [FromQuery] NewsCategory? category,
        [FromQuery] Guid? sourceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = db.Articles.AsNoTracking()
            .Include(a => a.Source)
            .Where(a => a.Source.IsActive);

        if (category is not null)
            q = q.Where(a => a.Category == category);
        if (sourceId is not null)
            q = q.Where(a => a.SourceId == sourceId);
        if (from is not null)
            q = q.Where(a => a.PublishedAt >= from.Value);
        if (to is not null)
            q = q.Where(a => a.PublishedAt <= to.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleResponse(
                a.Id,
                a.SourceId,
                a.Title,
                a.Summary,
                a.Url,
                a.PublishedAt,
                a.ImageUrl,
                a.Category))
            .ToListAsync(ct);

        return Ok(new PagedArticlesResponse(items, page, pageSize, total));
    }

    [HttpPost]
    public async Task<ActionResult<ArticleResponse>> Create(
        [FromBody] CreateArticleRequest request,
        CancellationToken ct = default)
    {
        var canonicalUrl = ArticleUrlDuplicateDetector.Normalize(request.Url);
        if (await db.Articles.AnyAsync(a => a.Url == canonicalUrl, ct))
            return Conflict(new { message = "Article URL must be unique." });

        var sourceExists = await db.Sources.AnyAsync(s => s.Id == request.SourceId, ct);
        if (!sourceExists)
            return BadRequest(new { message = "Source not found." });

        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceId = request.SourceId,
            Title = request.Title,
            Summary = request.Summary,
            Url = canonicalUrl,
            PublishedAt = request.PublishedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.PublishedAt, DateTimeKind.Utc)
                : request.PublishedAt.ToUniversalTime(),
            ImageUrl = request.ImageUrl,
            Category = request.Category
        };

        db.Articles.Add(article);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, new ArticleResponse(
            article.Id,
            article.SourceId,
            article.Title,
            article.Summary,
            article.Url,
            article.PublishedAt,
            article.ImageUrl,
            article.Category));
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    public async Task<ActionResult<ArticleResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var article = await db.Articles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (article is null)
            return NotFound();

        return Ok(new ArticleResponse(
            article.Id,
            article.SourceId,
            article.Title,
            article.Summary,
            article.Url,
            article.PublishedAt,
            article.ImageUrl,
            article.Category));
    }

    [HttpGet("trending")]
    public async Task<ActionResult<IReadOnlyList<TrendingArticleResponse>>> Trending(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var windowStart = DateTime.UtcNow.AddDays(-7);

        var rows = await (
            from b in db.Bookmarks.AsNoTracking()
            where b.CreatedAt >= windowStart
            join a in db.Articles.AsNoTracking() on b.ArticleId equals a.Id
            join s in db.Sources.AsNoTracking() on a.SourceId equals s.Id
            where s.IsActive
            group b by a.Id into g
            select new { ArticleId = g.Key, Cnt = g.Count() }
        ).OrderByDescending(x => x.Cnt)
            .ThenBy(x => x.ArticleId)
            .Take(limit)
            .ToListAsync(ct);

        var result = rows.Select(x => new TrendingArticleResponse(x.ArticleId, x.Cnt)).ToList();
        return Ok(result);
    }
}
