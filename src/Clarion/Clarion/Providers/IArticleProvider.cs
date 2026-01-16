using Clarion.Models;

namespace Clarion.Providers;

public interface IArticleProvider
{
    Task<IReadOnlyList<ArticleSummary>> GetArticlesAsync(string ticker, CancellationToken cancellationToken = default);

    Task<Article> GetArticleAsync(string sourceArticleId, CancellationToken cancellationToken = default);
}