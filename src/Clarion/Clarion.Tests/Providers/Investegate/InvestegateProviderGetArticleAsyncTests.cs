using System.Net;
using Clarion.Providers.Investegate;
using Clarion.Tests.Providers.Investegate.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Clarion.Tests.Providers.Investegate;

public class InvestegateProviderGetArticleAsyncTests
{
    private const string ValidArticleDetailHtml = """
        <!DOCTYPE html>
        <html>
        <head><title>Announcement</title></head>
        <body>
            <h1>Trading Update - Q4 2025</h1>
            <div class="art-board">
                <img src="https://tracker.example.com/pixel.gif" />
                <div>
                    <p>The company is pleased to announce strong trading results.</p>
                    <p>Revenue increased by 25% year-over-year.</p>
                    <img src="https://example.com/chart.png" />
                </div>
            </div>
        </body>
        </html>
        """;

    [Fact]
    public async Task GetArticleAsync_WithValidHtml_ReturnsArticle()
    {
        // Arrange
        var handler = CreateMockHandler(ValidArticleDetailHtml);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        result.SourceArticleId.Should().Be("test-12345");
        result.Source.Should().Be("investegate");
        result.Headline.Should().Be("Trading Update - Q4 2025");
        result.Url.Should().Be("https://www.investegate.co.uk/announcement/test-12345");
        result.FetchedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Body text should have normalized whitespace and exclude the tracker image
        result.BodyText.Should().Contain("The company is pleased to announce strong trading results.");
        result.BodyText.Should().Contain("Revenue increased by 25% year-over-year.");

        // Body HTML should not contain the first (tracker) image
        result.BodyHtml.Should().NotContain("tracker.example.com");
        result.BodyHtml.Should().Contain("https://example.com/chart.png");
        result.BodyHtml.Should().Contain("<p>The company is pleased to announce strong trading results.</p>");
    }

    [Fact]
    public async Task GetArticleAsync_WithTrackerImage_RemovesFirstImage()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test Article</h1>
                <div class="art-board">
                    <img src="https://tracker.example.com/pixel.gif" />
                    <div>
                        <p>Content here.</p>
                        <img src="https://example.com/legitimate-image.png" />
                    </div>
                </div>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        result.BodyHtml.Should().NotContain("tracker.example.com");
        result.BodyHtml.Should().Contain("legitimate-image.png");
    }

    [Fact]
    public async Task GetArticleAsync_WithNoImages_DoesNotThrow()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test Article</h1>
                <div class="art-board">
                    <img src="https://tracker.example.com/pixel.gif" />
                    <div>
                        <p>Content without images.</p>
                    </div>
                </div>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        result.BodyText.Should().Contain("Content without images.");
        result.BodyHtml.Should().Contain("<p>Content without images.</p>");
    }

    [Fact]
    public async Task GetArticleAsync_WithMultipleWhitespace_NormalizesBodyText()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test Article</h1>
                <div class="art-board">
                    <img src="https://tracker.example.com/pixel.gif" />
                    <div>
                        <p>This   has     multiple

                        spaces    and    newlines.</p>
                    </div>
                </div>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        // Multiple spaces and newlines should be normalized to single spaces
        result.BodyText.Should().Be("This has multiple spaces and newlines.");
    }

    [Fact]
    public async Task GetArticleAsync_WithComplexHtml_PreservesBodyHtmlStructure()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Complex Article</h1>
                <div class="art-board">
                    <img src="tracker.gif" />
                    <div>
                        <div class="section">
                            <h2>Section Title</h2>
                            <p>Paragraph with <strong>bold</strong> and <em>italic</em> text.</p>
                            <ul>
                                <li>Item 1</li>
                                <li>Item 2</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        result.BodyHtml.Should().NotContain("tracker.gif");
        result.BodyHtml.Should().Contain("<h2>Section Title</h2>");
        result.BodyHtml.Should().Contain("<strong>bold</strong>");
        result.BodyHtml.Should().Contain("<em>italic</em>");
        result.BodyHtml.Should().Contain("<ul>");
        result.BodyHtml.Should().Contain("<li>Item 1</li>");
    }

    [Fact]
    public async Task GetArticleAsync_WithHtmlEntities_DecodesCorrectly()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test &amp; Article</h1>
                <div class="art-board">
                    <img src="https://tracker.example.com/pixel.gif" />
                    <div>
                        <p>Revenue &gt; £1M &amp; &lt; £2M.</p>
                    </div>
                </div>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("test-12345");

        // Assert
        result.Headline.Should().Be("Test & Article");
        result.BodyText.Should().Contain("Revenue > £1M & < £2M.");
    }

    [Fact]
    public async Task GetArticleAsync_BuildsCorrectUrl()
    {
        // Arrange
        string? requestedUrl = null;
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            requestedUrl = request.RequestUri?.ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidArticleDetailHtml)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        await provider.GetArticleAsync("test-article-id-123");

        // Assert
        requestedUrl.Should().Be("https://www.investegate.co.uk/announcement/test-article-id-123");
    }

    [Fact]
    public async Task GetArticleAsync_WithMissingH1_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <div>No headline here</div>
                <body>
                    <p>Content without h1.</p>
                </body>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticleAsync("test-12345");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to find headline element");
    }

    [Fact]
    public async Task GetArticleAsync_WithSingleBody_ThrowsInvalidOperationException()
    {
        // Arrange - Only one body tag
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test Article</h1>
                <p>Content in main body only.</p>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticleAsync("test-12345");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to find announcement content (tracker image not found)");
    }

    [Fact]
    public async Task GetArticleAsync_WithNoBody_ThrowsInvalidOperationException()
    {
        // Arrange - No body tags at all
        var html = """
            <!DOCTYPE html>
            <html>
                <h1>Test Article</h1>
                <p>Content without body.</p>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticleAsync("test-12345");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to find announcement content (tracker image not found)");
    }

    [Fact]
    public async Task GetArticleAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidArticleDetailHtml)
            });
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticleAsync("test-12345", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetArticleAsync_WithHttpRequestException_PropagatesException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            throw new HttpRequestException("Network error");
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticleAsync("test-12345");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }

    private static MockHttpMessageHandler CreateMockHandler(string responseContent)
    {
        return new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };
            return Task.FromResult(response);
        });
    }
}
