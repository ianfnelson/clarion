using System.Globalization;
using AngleSharp;
using Clarion.Models;

namespace Clarion.Providers.Investegate;

public partial class InvestegateProvider(HttpClient httpClient) : IArticleProvider
{
    private const string BaseUrl = "https://www.investegate.co.uk";
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/London");

    public async Task<IReadOnlyList<ArticleSummary>> GetArticlesAsync(string ticker, 
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/company/{ticker}?perPage=1000";
        var html = await httpClient.GetStringAsync(url, cancellationToken);

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

        var rows = document.QuerySelectorAll("tbody tr");
        var articles = new List<ArticleSummary>();

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("td");
            if (cells.Length < 4)
                continue;

            // Extract date and time from first two cells
            var dateText = cells[0].TextContent.Trim();
            var timeText = cells[1].TextContent.Trim();

            // Parse as UK time and convert to UTC
            var dateTimeText = $"{dateText} {timeText}";
            if (!DateTime.TryParseExact(dateTimeText, "d MMM yyyy hh:mm tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var ukDateTime))
            {
                throw new InvalidOperationException($"Failed to parse date/time: {dateTimeText}");
            }

            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(ukDateTime, UkTimeZone);

            // Extract headline and URL from fourth cell
            var headlineLink = cells[3].QuerySelector("a");
            if (headlineLink == null)
                continue;

            var headline = headlineLink.TextContent.Trim();
            var href = headlineLink.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
                continue;

            // Extract article ID from URL (last segment after final /)
            var articleId = href.Split('/').Last();
            var fullUrl = href.StartsWith("http") ? href : $"{BaseUrl}{href}";

            articles.Add(new ArticleSummary
            {
                SourceArticleId = articleId,
                Source = "investegate",
                Ticker = ticker,
                Headline = headline,
                PublishedUtc = utcDateTime,
                Url = fullUrl
            });
        }

        return articles;
    }

    public async Task<Article> GetArticleAsync(string sourceArticleId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/announcement/{sourceArticleId}";
        var html = await httpClient.GetStringAsync(url, cancellationToken);

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

        // Extract headline from h1 in main document
        var headlineElement = document.QuerySelector("h1");
        if (headlineElement == null)
            throw new InvalidOperationException("Failed to find headline element");

        var headline = WhitespaceRegex().Replace(headlineElement.TextContent, " ").Trim();

        // Try different parsing strategies based on article format
        var (bodyHtml, bodyText) = TryParseNewsWindowWithTrackerFormat(document)
                                   ?? TryParseNewsWindowFormat(document)
                                   ?? throw new InvalidOperationException("Failed to parse article content - no recognized format found");

        return new Article
        {
            SourceArticleId = sourceArticleId,
            Source = "investegate",
            Headline = headline,
            RetrievedUtc = DateTime.UtcNow,
            Url = url,
            BodyHtml = bodyHtml,
            BodyText = bodyText
        };
    }

    /// <summary>
    /// Attempts to parse newer article format where news-window contains a nested HTML document with a tracker image
    /// </summary>
    private static (string bodyHtml, string bodyText)? TryParseNewsWindowWithTrackerFormat(AngleSharp.Dom.IDocument document)
    {
        var newsWindow = document.QuerySelector("div.news-window");
        if (newsWindow == null)
            return null;

        // Check if there's a tracker image within the news-window
        var trackerImage = newsWindow.QuerySelector("img[src*='tracker']") ??
                          newsWindow.QuerySelector("img[src*='rns-distribution']");
        if (trackerImage == null)
            return null;

        // Get the parent container that holds the announcement content
        var announcementContainer = trackerImage.ParentElement;
        if (announcementContainer == null)
            return null;

        // Create a virtual element to hold just the announcement content
        var announcementContent = document.CreateElement("div");

        // Copy all children from the container, starting from the tracker image
        var children = announcementContainer.Children.ToArray();
        var trackerIndex = Array.IndexOf(children, trackerImage);

        for (var i = trackerIndex; i < children.Length; i++)
        {
            announcementContent.AppendChild(children[i].Clone());
        }

        // Remove the first child (tracker image) from our virtual element
        announcementContent.Children[0].Remove();

        // Get cleaned HTML
        var bodyHtml = announcementContent.InnerHtml;

        // Convert to plain text
        var bodyText = announcementContent.TextContent;

        // Normalize whitespace
        bodyText = WhitespaceRegex().Replace(bodyText, " ").Trim();

        return (bodyHtml, bodyText);
    }

    /// <summary>
    /// Attempts to parse older article format where content is directly in a div with class "news-window"
    /// </summary>
    private static (string bodyHtml, string bodyText)? TryParseNewsWindowFormat(AngleSharp.Dom.IDocument document)
    {
        var newsWindow = document.QuerySelector("div.news-window");
        if (newsWindow == null)
            return null;

        var bodyHtml = newsWindow.InnerHtml;
        var bodyText = newsWindow.TextContent;
        bodyText = WhitespaceRegex().Replace(bodyText, " ").Trim();

        return (bodyHtml, bodyText);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
    private static partial System.Text.RegularExpressions.Regex WhitespaceRegex();
}