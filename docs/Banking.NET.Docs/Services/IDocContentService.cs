using Banking.NET.Docs.Models;

namespace Banking.NET.Docs.Services;

/// <summary>Loads and queries the generated content index (<c>data/content-index.json</c>) and renders individual markdown pages.</summary>
public interface IDocContentService
{
    /// <summary>Loads the content index on first call; subsequent calls are no-ops.</summary>
    Task InitializeAsync();

    /// <summary>Returns the navigation sections built from the content index, grouped by category and ordered.</summary>
    List<NavSection> GetNavSections();

    /// <summary>Searches titles, headings and body text for <paramref name="query"/>, returning the best matches.</summary>
    List<SearchEntry> Search(string query);

    /// <summary>Fetches and renders the markdown page at <paramref name="slug"/>, or null if it doesn't exist.</summary>
    Task<DocArticle?> GetArticleAsync(string slug);
}
