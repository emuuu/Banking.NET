namespace Banking.NET.Docs.Models;

/// <summary>One search result: a content page matched against a query.</summary>
public class SearchEntry
{
    /// <summary>The page's URL slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The page title.</summary>
    public string Title { get; set; } = "";

    /// <summary>The page body as plain text, used to score and highlight matches.</summary>
    public string PlainText { get; set; } = "";

    /// <summary>The page's headings, used to score matches.</summary>
    public List<string> Headings { get; set; } = [];
}
