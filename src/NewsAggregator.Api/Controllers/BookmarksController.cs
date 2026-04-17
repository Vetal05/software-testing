using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Entities;
using NewsAggregator.Api.Models.Dtos;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/bookmarks")]
public class BookmarksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookmarkResponse>>> ListForUser(
        [FromQuery] Guid userId,
        CancellationToken ct = default)
    {
        var items = await db.Bookmarks.AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookmarkResponse(b.Id, b.UserId, b.ArticleId, b.CreatedAt, b.Notes))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<BookmarkResponse>> Create(
        [FromBody] CreateBookmarkRequest request,
        CancellationToken ct = default)
    {
        if (!await db.Articles.AnyAsync(a => a.Id == request.ArticleId, ct))
            return BadRequest(new { message = "Article not found." });

        if (await db.Bookmarks.AnyAsync(
                b => b.UserId == request.UserId && b.ArticleId == request.ArticleId, ct))
            return Conflict(new { message = "Bookmark already exists for this user and article." });

        var createdAt = request.CreatedAt ?? DateTime.UtcNow;
        if (createdAt.Kind == DateTimeKind.Unspecified)
            createdAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
        else
            createdAt = createdAt.ToUniversalTime();

        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ArticleId = request.ArticleId,
            CreatedAt = createdAt,
            Notes = request.Notes
        };

        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListForUser), new { userId = bookmark.UserId },
            new BookmarkResponse(bookmark.Id, bookmark.UserId, bookmark.ArticleId, bookmark.CreatedAt, bookmark.Notes));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var bookmark = await db.Bookmarks.FindAsync([id], ct);
        if (bookmark is null)
            return NotFound();

        db.Bookmarks.Remove(bookmark);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
