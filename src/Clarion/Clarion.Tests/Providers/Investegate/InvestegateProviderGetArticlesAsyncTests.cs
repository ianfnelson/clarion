using System.Net;
using Clarion.Providers.Investegate;
using Clarion.Tests.Providers.Investegate.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Clarion.Tests.Providers.Investegate;

public class InvestegateProviderGetArticlesAsyncTests
{
    private const string ValidArticleListHtml = """
        <!DOCTYPE html>
        <html>
        <body>
            <table>
                <tbody>
                    <tr>
                        <td>15 Jan 2026</td>
                        <td>07:00 AM</td>
                        <td>RNS</td>
                        <td><a href="/announcement/test-12345">Trading Update</a></td>
                    </tr>
                    <tr>
                        <td>14 Jan 2026</td>
                        <td>04:30 PM</td>
                        <td>RNS</td>
                        <td><a href="/announcement/test-67890">Annual Results</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """;

    [Fact]
    public async Task GetArticlesAsync_WithValidHtml_ReturnsArticleSummaries()
    {
        // Arrange
        var handler = CreateMockHandler(ValidArticleListHtml);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(2);

        result[0].Headline.Should().Be("Trading Update");
        result[0].SourceArticleId.Should().Be("test-12345");
        result[0].Source.Should().Be("investegate");
        result[0].Ticker.Should().Be("TEST");
        result[0].Url.Should().Be("https://www.investegate.co.uk/announcement/test-12345");

        result[1].Headline.Should().Be("Annual Results");
        result[1].SourceArticleId.Should().Be("test-67890");
        result[1].Source.Should().Be("investegate");
        result[1].Ticker.Should().Be("TEST");
    }

    [Fact]
    public async Task GetArticlesAsync_WithSingleArticle_ParsesDateTimeCorrectly()
    {
        // Arrange - Testing GMT period (January)
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-12345">Trading Update</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        // During GMT (winter), UK time equals UTC
        result[0].PublishedUtc.Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetArticlesAsync_WithMultipleArticles_MaintainsOrder()
    {
        // Arrange
        var handler = CreateMockHandler(ValidArticleListHtml);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(2);
        result[0].Headline.Should().Be("Trading Update");
        result[1].Headline.Should().Be("Annual Results");
        // Verify order is preserved from the HTML
        result[0].PublishedUtc.Should().BeAfter(result[1].PublishedUtc);
    }

    [Fact]
    public async Task GetArticlesAsync_WithRelativeUrl_BuildsAbsoluteUrl()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-12345">Trading Update</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://www.investegate.co.uk/announcement/test-12345");
    }

    [Fact]
    public async Task GetArticlesAsync_WithAbsoluteUrl_PreservesUrl()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td><a href="https://www.investegate.co.uk/announcement/test-12345">Trading Update</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://www.investegate.co.uk/announcement/test-12345");
    }

    [Fact]
    public async Task GetArticlesAsync_BuildsCorrectUrl()
    {
        // Arrange
        string? requestedUrl = null;
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            requestedUrl = request.RequestUri?.ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidArticleListHtml)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        await provider.GetArticlesAsync("TSLA");

        // Assert
        requestedUrl.Should().Be("https://www.investegate.co.uk/company/TSLA?perPage=1000");
    }

    [Fact]
    public async Task GetArticlesAsync_WithRowsHavingLessThanFourCells_SkipsInvalidRows()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                        </tr>
                        <tr>
                            <td>14 Jan 2026</td>
                            <td>04:30 PM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-67890">Valid Article</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Headline.Should().Be("Valid Article");
    }

    [Fact]
    public async Task GetArticlesAsync_WithRowMissingAnchorTag_SkipsRow()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td>No Link Here</td>
                        </tr>
                        <tr>
                            <td>14 Jan 2026</td>
                            <td>04:30 PM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-67890">Valid Article</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Headline.Should().Be("Valid Article");
    }

    [Fact]
    public async Task GetArticlesAsync_WithAnchorMissingHref_SkipsRow()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>15 Jan 2026</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td><a>No Href Here</a></td>
                        </tr>
                        <tr>
                            <td>14 Jan 2026</td>
                            <td>04:30 PM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-67890">Valid Article</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Headline.Should().Be("Valid Article");
    }

    [Fact]
    public async Task GetArticlesAsync_WithEmptyTable_ReturnsEmptyList()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArticlesAsync_WithNoTbody_ReturnsEmptyList()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArticlesAsync_WithWhitespaceInCells_TrimsCorrectly()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>  15 Jan 2026  </td>
                            <td>  07:00 AM  </td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-12345">  Trading Update  </a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var result = await provider.GetArticlesAsync("TEST");

        // Assert
        result.Should().HaveCount(1);
        result[0].Headline.Should().Be("Trading Update");
        result[0].PublishedUtc.Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetArticlesAsync_WithInvalidDateFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <body>
                <table>
                    <tbody>
                        <tr>
                            <td>Invalid Date</td>
                            <td>07:00 AM</td>
                            <td>RNS</td>
                            <td><a href="/announcement/test-12345">Trading Update</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>
            """;

        var handler = CreateMockHandler(html);
        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticlesAsync("TEST");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to parse date/time: Invalid Date 07:00 AM");
    }

    [Fact]
    public async Task GetArticlesAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidArticleListHtml)
            });
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticlesAsync("TEST", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetArticlesAsync_WithHttpRequestException_PropagatesException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            throw new HttpRequestException("Network error");
        });

        var httpClient = new HttpClient(handler);
        var provider = new InvestegateProvider(httpClient);

        // Act
        var act = async () => await provider.GetArticlesAsync("TEST");

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
