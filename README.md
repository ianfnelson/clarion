# Clarion

A .NET library for fetching and parsing stock market announcements from Investegate.

## Quick Start

```csharp
using Clarion;

// Create a Clarion client
var client = ClarionClient.Create();

// Get article summaries for a ticker
var articles = await client.GetArticlesAsync("KNOS");

foreach (var summary in articles)
{
    Console.WriteLine($"{summary.PublishedUtc}: {summary.Headline}");
}

// Get full article details
var article = await client.GetArticleAsync(articles[0].SourceArticleId);
Console.WriteLine(article.BodyText);
```

## Features

- Fetch article summaries by ticker symbol
- Retrieve full article content including HTML and plain text
- Built on .NET 10.0 with nullable reference types
- Uses AngleSharp for robust HTML parsing
- Simple, intuitive API

## API Reference

### ClarionClient

The main entry point for the library.

#### Methods

**`Create()`**
Creates a new instance of ClarionClient with default configuration.

**`GetArticlesAsync(string ticker, CancellationToken cancellationToken = default)`**
Fetches a list of article summaries for the specified ticker.

**`GetArticleAsync(string sourceArticleId, CancellationToken cancellationToken = default)`**
Fetches the full article details for a specific article ID.

### Models

**ArticleSummary**
- `SourceArticleId` - Unique identifier for the article
- `Source` - Source system (e.g., "investegate")
- `Ticker` - Stock ticker symbol
- `Headline` - Article headline
- `PublishedUtc` - Publication date and time (UTC)
- `Url` - Canonical URL to the article

**Article**
- `SourceArticleId` - Unique identifier for the article
- `Source` - Source system identifier
- `Headline` - Article headline
- `RetrievedUtc` - When the article was fetched (UTC)
- `Url` - Canonical URL to the article
- `BodyHtml` - Raw HTML content
- `BodyText` - Plain text version with normalized whitespace

## Requirements

- .NET 10.0 or later

## Links

- [GitHub Repository](https://github.com/ianfnelson/clarion)
- [NuGet Package](https://www.nuget.org/packages/Clarion/)
- [Investegate](https://www.investegate.co.uk/)
