namespace Clarion.Models;

public sealed class ArticleSummary
{
    /// <summary>
    /// Identifier of the article in the source system.
    /// Opaque to consumers; pass back to Clarion to fetch the full article.
    /// </summary>
    public required string SourceArticleId { get; init; }

    /// <summary>
    /// Identifier of the source (e.g. "investegate").
    /// Useful for diagnostics and future multi-source scenarios.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Ticker or identifier the article relates to.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Article headline as published.
    /// </summary>
    public required string Headline { get; init; }

    /// <summary>
    /// When the article was published, in UTC.
    /// </summary>
    public required DateTime PublishedAtUtc { get; init; }

    /// <summary>
    /// Canonical URL of the article, if available.
    /// </summary>
    public string? Url { get; init; }
}