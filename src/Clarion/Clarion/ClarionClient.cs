using Clarion.Models;
using Clarion.Providers;
using Clarion.Providers.Investegate;

namespace Clarion;

public sealed class ClarionClient
{
    private readonly IArticleProvider _provider;

    private ClarionClient(IArticleProvider provider)
    {
        _provider = provider;
    }
    
    public static ClarionClient Create()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var provider = new InvestegateProvider(httpClient);

        return new ClarionClient(provider);
    }

    public Task<IReadOnlyList<ArticleSummary>> GetArticlesAsync(string ticker,
        CancellationToken cancellationToken = default)
    {
        return _provider.GetArticlesAsync(ticker, cancellationToken);
    }

    public Task<Article> GetArticleAsync(string sourceArticleId,
        CancellationToken cancellationToken = default)
    {
        return _provider.GetArticleAsync(sourceArticleId, cancellationToken);
    }
}