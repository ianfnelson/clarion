namespace Clarion.Models;

public sealed class Article
{
    /// <summary>
    /// Identifier of the article in the source system.
    /// Matches ArticleSummary.SourceArticleId.
    /// </summary>
    public required string SourceArticleId { get; init; }

    /// <summary>
    /// Identifier of the source system.
    /// </summary>
    public required string Source { get; init; }
    
    /// <summary>
    /// Article headline.
    /// </summary>
    public required string Headline { get; init; }

    /// <summary>
    /// When Clarion fetched the article, in UTC.
    /// </summary>
    public required DateTime FetchedAtUtc { get; init; }

    /// <summary>
    /// Canonical URL of the article.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Raw HTML body as published.
    /// Stored for reprocessing and forensic purposes.
    /// </summary>
    public required string BodyHtml { get; init; }

    /// <summary>
    /// Plain-text version of the article body.
    /// Normalised whitespace, no markup.
    /// </summary>
    public required string BodyText { get; init; }
}