using System.Net;
using Clarion.Providers.Investegate;
using Clarion.Tests.Providers.Investegate.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Clarion.Tests.Providers.Investegate;

public class InvestegateProviderGetArticleAsyncTests
{
    [Fact]
    public async Task GetArticleAsync_WithRealInvestegateHtml_9378240_ParsesCorrectly()
    {
        // Arrange - newer format with tracker image
        var htmlPath = Path.Combine("Providers", "Investegate", "TestData", "Articles", "9378240.html");
        var html = await File.ReadAllTextAsync(htmlPath);

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("9378240");

        // Assert
        result.SourceArticleId.Should().Be("9378240");
        result.Source.Should().Be("investegate");
        result.Headline.Should().Be("Transaction in Own Shares");
        result.Url.Should().Be("https://www.investegate.co.uk/announcement/9378240");
        result.RetrievedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Body text should contain key announcement content
        result.BodyText.Should().Contain("Kainos Group plc");
        result.BodyText.Should().Contain("purchased the following number of its ordinary shares");
        result.BodyText.Should().Contain("Investec Bank plc");

        // Body HTML should not contain the tracker image
        result.BodyHtml.Should().NotContain("tracker.live.rns-distribution.com");

        // Body HTML should contain actual content
        result.BodyHtml.Should().Contain("Kainos Group plc");
    }

    [Fact]
    public async Task GetArticleAsync_WithRealInvestegateHtml_4053574_ParsesCorrectly()
    {
        // Arrange - older format with news-window div
        var htmlPath = Path.Combine("Providers", "Investegate", "TestData", "Articles", "4053574.html");
        var html = await File.ReadAllTextAsync(htmlPath);

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("4053574");

        // Assert
        result.SourceArticleId.Should().Be("4053574");
        result.Source.Should().Be("investegate");
        result.Headline.Should().Be("Issue of Warrants");
        result.Url.Should().Be("https://www.investegate.co.uk/announcement/4053574");
        result.RetrievedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Body text should contain key announcement content
        result.BodyText.Should().Contain("Audioboom Group plc");
        result.BodyText.Should().Contain("issued 2,000,000 warrants");
        result.BodyText.Should().Contain("exercise price of 12.5p");

        // Body HTML should contain actual content
        result.BodyHtml.Should().Contain("Audioboom Group plc");
        result.BodyHtml.Should().Contain("Issue of Warrants");
    }

    [Fact]
    public async Task GetArticleAsync_WithRealInvestegateHtml_8130950_ParsesCorrectly()
    {
        // Arrange - EQS announcement format with news-window div
        var htmlPath = Path.Combine("Providers", "Investegate", "TestData", "Articles", "8130950.html");
        var html = await File.ReadAllTextAsync(htmlPath);

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticleAsync("8130950");

        // Assert
        result.SourceArticleId.Should().Be("8130950");
        result.Source.Should().Be("investegate");
        result.Headline.Should().Be("Edison issues update on Seraphim Space Investment Trust (SSIT): Several key holdings funded to break-even");
        result.Url.Should().Be("https://www.investegate.co.uk/announcement/8130950");
        result.RetrievedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Body text should contain key announcement content
        result.BodyText.Should().Contain("Edison Investment Research Limited");
        result.BodyText.Should().Contain("London, UK, 10 April 2024");
        result.BodyText.Should().Contain("Seraphim Space Investment Trust");

        // Body HTML should contain actual content
        result.BodyHtml.Should().Contain("Edison Investment Research Limited");
        result.BodyHtml.Should().Contain("eqs-announcement");
    }

    [Fact]
    public async Task GetArticleAsync_WithMissingH1_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <div class="news-window">
                    <p>Content without headline</p>
                </div>
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
    public async Task GetArticleAsync_WithUnrecognizedFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Test Article</h1>
                <div class="some-other-class">
                    <p>Content in unrecognized format.</p>
                </div>
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
            .WithMessage("Failed to parse article content - no recognized format found");
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
                Content = new StringContent("<html><body><h1>Test</h1><div class=\"news-window\">Content</div></body></html>")
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
