namespace Banking.NET.Docs.Models;

/// <summary>One entry of the generated content index (<c>data/content-index.json</c>), deserialized client-side.</summary>
public class ContentIndex
{
    /// <summary>The page's URL slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The page title.</summary>
    public string Title { get; set; } = "";

    /// <summary>The navigation category.</summary>
    public string Category { get; set; } = "";

    /// <summary>The sort order within the category.</summary>
    public int Order { get; set; }

    /// <summary>The page description.</summary>
    public string Description { get; set; } = "";

    /// <summary>The page's h2/h3 headings, in document order.</summary>
    public List<string> Headings { get; set; } = [];

    /// <summary>The page body as plain text, used for full-text search.</summary>
    public string SearchText { get; set; } = "";
}
