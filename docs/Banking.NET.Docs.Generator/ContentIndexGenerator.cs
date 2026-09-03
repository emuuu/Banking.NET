using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Banking.NET.Docs.Generator;

/// <summary>Converts the markdown files under <c>wwwroot/content</c> into a searchable, navigable content index.</summary>
public partial class ContentIndexGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers()
        .Build();

    /// <summary>Reads every markdown file under <paramref name="contentDir"/>, renders it, and writes the resulting index as JSON to <paramref name="outputPath"/>.</summary>
    /// <param name="contentDir">The directory to scan recursively for <c>*.md</c> files.</param>
    /// <param name="outputPath">The file the generated JSON index is written to.</param>
    /// <returns>The generated entries, for callers that also need them (e.g. to build the sitemap and static pages).</returns>
    public async Task<List<ContentIndexEntry>> GenerateAsync(string contentDir, string outputPath)
    {
        var entries = new List<ContentIndexEntry>();

        if (!Directory.Exists(contentDir))
        {
            Console.WriteLine($"  Content directory not found: {contentDir}. Creating empty index.");
            await File.WriteAllTextAsync(outputPath, "[]").ConfigureAwait(false);
            return entries;
        }

        var mdFiles = Directory.GetFiles(contentDir, "*.md", SearchOption.AllDirectories);

        foreach (var filePath in mdFiles)
        {
            var relativePath = Path.GetRelativePath(contentDir, filePath);
            var slug = relativePath.Replace('\\', '/').Replace(".md", "");

            var markdown = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var (frontMatter, body) = ParseFrontMatter(markdown);

            if (frontMatter is null) continue;

            var html = Markdown.ToHtml(body, Pipeline);
            var headings = ExtractHeadings(html);
            var plainText = StripToPlainText(body);

            entries.Add(new ContentIndexEntry
            {
                Slug = slug,
                Title = frontMatter.Title ?? Path.GetFileNameWithoutExtension(filePath),
                Category = frontMatter.Category ?? InferCategory(slug),
                Order = frontMatter.Order,
                Description = frontMatter.Description ?? "",
                Headings = headings,
                SearchText = plainText[..Math.Min(plainText.Length, 500)],
                HtmlContent = html
            });
        }

        entries = entries.OrderBy(e => e.Category, StringComparer.Ordinal).ThenBy(e => e.Order).ThenBy(e => e.Title, StringComparer.Ordinal).ToList();

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
        Console.WriteLine($"  Generated {entries.Count} content entries -> {Path.GetFileName(outputPath)}");
        return entries;
    }

    private static (FrontMatter? FrontMatter, string Body) ParseFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---", StringComparison.Ordinal))
            return (null, markdown);

        var endIndex = markdown.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (null, markdown);

        var yaml = markdown[3..endIndex].Trim();
        var body = markdown[(endIndex + 3)..].TrimStart();

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var fm = deserializer.Deserialize<FrontMatter>(yaml);
            return (fm, body);
        }
        catch (YamlDotNet.Core.YamlException)
        {
            return (null, body);
        }
    }

    private static List<string> ExtractHeadings(string html)
    {
        var headings = new List<string>();
        var matches = HeadingRegex().Matches(html);
        foreach (Match match in matches)
        {
            headings.Add(StripHtmlTags(match.Groups[1].Value));
        }
        return headings;
    }

    private static string StripToPlainText(string markdown)
    {
        var text = markdown;
        text = CodeBlockRegex().Replace(text, " ");
        text = InlineCodeRegex().Replace(text, "$1");
        text = HeadingMarkerRegex().Replace(text, "");
        text = LinkRegex().Replace(text, "$1");
        text = ImageRegex().Replace(text, "");
        text = BoldItalicRegex().Replace(text, "$1");
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text;
    }

    private static string StripHtmlTags(string html) =>
        HtmlTagRegex().Replace(html, "").Trim();

    private static string InferCategory(string slug)
    {
        var parts = slug.Split('/');
        if (parts.Length < 2) return "General";
        return parts[0] switch
        {
            "getting-started" => "Getting Started",
            "guides" => "Guides",
            _ => parts[0]
        };
    }

    [GeneratedRegex(@"<h[2-3][^>]*>(.*?)</h[2-3]>", RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex HeadingMarkerRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]+\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"[*_]{1,3}([^*_]+)[*_]{1,3}")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

/// <summary>The YAML front matter block at the top of a content markdown file.</summary>
public class FrontMatter
{
    /// <summary>The page title; falls back to the file name when absent.</summary>
    public string? Title { get; set; }

    /// <summary>The navigation category; falls back to the top-level content folder when absent.</summary>
    public string? Category { get; set; }

    /// <summary>The sort order within the category.</summary>
    public int Order { get; set; }

    /// <summary>The page description, used for meta tags and search results.</summary>
    public string? Description { get; set; }
}

/// <summary>One entry in the generated content index: metadata and rendered content for a single markdown page.</summary>
public class ContentIndexEntry
{
    /// <summary>The page's URL slug, derived from its path under the content directory.</summary>
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

    /// <summary>The rendered HTML body. Not part of the JSON content index; consumed directly by <see cref="StaticHtmlGenerator"/>.</summary>
    [JsonIgnore]
    public string HtmlContent { get; set; } = "";
}
