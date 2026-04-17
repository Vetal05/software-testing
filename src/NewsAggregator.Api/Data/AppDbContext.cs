using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Entities;
using NewsAggregator.Core;

namespace NewsAggregator.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Source>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => x.Url).IsUnique();
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(8000).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(2000);
            e.HasIndex(x => x.Url).IsUnique();
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);

            e.HasOne(x => x.Source)
                .WithMany(s => s.Articles)
                .HasForeignKey(x => x.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bookmark>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasIndex(x => new { x.UserId, x.ArticleId }).IsUnique();

            e.HasOne(x => x.Article)
                .WithMany(a => a.Bookmarks)
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
