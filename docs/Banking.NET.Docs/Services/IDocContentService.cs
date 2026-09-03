using Banking.NET.Docs.Models;

namespace Banking.NET.Docs.Services;

public interface IDocContentService
{
    Task InitializeAsync();
    List<NavSection> GetNavSections();
    List<SearchEntry> Search(string query);
    Task<DocArticle?> GetArticleAsync(string slug);
}
