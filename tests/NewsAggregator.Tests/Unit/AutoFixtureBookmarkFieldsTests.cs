using AutoFixture;
using AutoFixture.Xunit2;

namespace NewsAggregator.Tests.Unit;

/// <summary>
/// Demonstrates AutoFixture for non-critical bookmark fields (Notes, CreatedAt) per assignment.
/// </summary>
public class AutoFixtureBookmarkFieldsTests
{
    public class BookmarkDraft
    {
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    [Theory, AutoData]
    public void AutoFixture_generates_notes_and_createdAt(BookmarkDraft draft)
    {
        Assert.False(string.IsNullOrWhiteSpace(draft.Notes));
        Assert.NotEqual(default, draft.CreatedAt);
    }

    [Fact]
    public void Fixture_build_with_explicit_ids()
    {
        var fixture = new Fixture();
        var draft = fixture.Build<BookmarkDraft>()
            .With(x => x.Notes, "saved for later")
            .With(x => x.CreatedAt, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Create();

        Assert.Equal("saved for later", draft.Notes);
        Assert.Equal(DateTimeKind.Utc, draft.CreatedAt.Kind);
    }
}
