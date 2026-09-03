namespace Banking.NET.Docs.Models;

/// <summary>A rendered content page, ready to display.</summary>
public class DocArticle
{
    /// <summary>The page title.</summary>
    public string Title { get; set; } = "";

    /// <summary>The page's URL slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The navigation category.</summary>
    public string Category { get; set; } = "";

    /// <summary>The sort order within the category.</summary>
    public int Order { get; set; }

    /// <summary>The page description.</summary>
    public string Description { get; set; } = "";

    /// <summary>The rendered HTML body.</summary>
    public string HtmlContent { get; set; } = "";

    /// <summary>The page's headings, extracted from the rendered HTML.</summary>
    public List<HeadingInfo> Headings { get; set; } = [];
}

/// <summary>One heading extracted from a rendered content page, used to build the "on this page" outline.</summary>
public class HeadingInfo
{
    /// <summary>The heading's element ID, used as an anchor target.</summary>
    public string Id { get; set; } = "";

    /// <summary>The heading text.</summary>
    public string Text { get; set; } = "";

    /// <summary>The heading level (2 or 3).</summary>
    public int Level { get; set; }
}
