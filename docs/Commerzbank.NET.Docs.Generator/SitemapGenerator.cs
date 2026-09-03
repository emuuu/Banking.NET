using System.Text;

namespace Commerzbank.NET.Docs.Generator;

public class SitemapGenerator
{
    private readonly string _baseUrl;

    public SitemapGenerator(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task GenerateAsync(string wwwrootPath, List<ContentIndexEntry> entries, IReadOnlyList<string> apiSlugs)
    {
        await GenerateSitemap(wwwrootPath, entries, apiSlugs).ConfigureAwait(false);
        await GenerateRobotsTxt(wwwrootPath).ConfigureAwait(false);
    }

    private async Task GenerateSitemap(string wwwrootPath, List<ContentIndexEntry> entries, IReadOnlyList<string> apiSlugs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        // Root
        AppendUrl(sb, $"{_baseUrl}/", "1.0");

        // Doc pages
        foreach (var entry in entries)
        {
            AppendUrl(sb, $"{_baseUrl}/docs/{entry.Slug}", "0.8");
        }

        // API Explorer
        AppendUrl(sb, $"{_baseUrl}/api", "0.7");

        // API reference pages (types and enums)
        foreach (var slug in apiSlugs)
        {
            AppendUrl(sb, $"{_baseUrl}/api/{slug}", "0.5");
        }

        sb.AppendLine("</urlset>");

        var outputPath = Path.Combine(wwwrootPath, "sitemap.xml");
        await File.WriteAllTextAsync(outputPath, sb.ToString()).ConfigureAwait(false);

        var totalUrls = 1 + entries.Count + 1 + apiSlugs.Count;
        Console.WriteLine($"  Generated sitemap.xml with {totalUrls} URLs");
    }

    private static void AppendUrl(StringBuilder sb, string loc, string priority)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{loc}</loc>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }

    private async Task GenerateRobotsTxt(string wwwrootPath)
    {
        var content = $"""
            User-agent: *
            Allow: /
            Sitemap: {_baseUrl}/sitemap.xml
            """;

        var outputPath = Path.Combine(wwwrootPath, "robots.txt");
        await File.WriteAllTextAsync(outputPath, content).ConfigureAwait(false);
        Console.WriteLine("  Generated robots.txt");
    }
}
