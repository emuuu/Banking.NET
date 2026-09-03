namespace Banking.NET.Docs.Models;

/// <summary>One category group in the content navigation menu (e.g. "Getting Started").</summary>
public class NavSection
{
    /// <summary>The category name.</summary>
    public string Category { get; set; } = "";

    /// <summary>The pages in this category, ordered.</summary>
    public List<NavItem> Items { get; set; } = [];
}

/// <summary>One page link within a <see cref="NavSection"/>.</summary>
public class NavItem
{
    /// <summary>The page title, as shown in the navigation menu.</summary>
    public string Title { get; set; } = "";

    /// <summary>The page's URL slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The sort order within the section.</summary>
    public int Order { get; set; }
}
