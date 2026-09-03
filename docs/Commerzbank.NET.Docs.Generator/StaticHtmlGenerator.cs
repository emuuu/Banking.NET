using System.Text.Json;
using System.Web;

namespace Commerzbank.NET.Docs.Generator;

public class StaticHtmlGenerator
{
    private readonly string _baseUrl;

    public StaticHtmlGenerator(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task GenerateAsync(string wwwrootPath, List<ContentIndexEntry> entries)
    {
        var count = 0;

        foreach (var entry in entries)
        {
            var dir = Path.Combine(wwwrootPath, "docs", entry.Slug);
            Directory.CreateDirectory(dir);
            var html = BuildDocPage(entry);
            await File.WriteAllTextAsync(Path.Combine(dir, "index.html"), html).ConfigureAwait(false);
            count++;
        }

        Console.WriteLine($"  Generated {count} static doc pages");

        await Generate404(wwwrootPath).ConfigureAwait(false);
        await InjectMetaTags(wwwrootPath).ConfigureAwait(false);
    }

    private string BuildDocPage(ContentIndexEntry entry)
    {
        var title = HttpUtility.HtmlEncode(entry.Title);
        var description = HttpUtility.HtmlEncode(entry.Description);
        var url = $"{_baseUrl}/docs/{entry.Slug}";
        var jsonLd = BuildJsonLd(entry);

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{title} - Commerzbank.NET Docs</title>
                <base href="/" />
                <meta name="description" content="{description}" />
                <link rel="canonical" href="{url}" />
                <meta property="og:type" content="article" />
                <meta property="og:title" content="{title} - Commerzbank.NET" />
                <meta property="og:description" content="{description}" />
                <meta property="og:url" content="{url}" />
                <meta property="og:site_name" content="Commerzbank.NET Docs" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="{title} - Commerzbank.NET" />
                <meta name="twitter:description" content="{description}" />
                <script type="application/ld+json">{jsonLd}</script>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                      integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />
                <link href="lib/prismjs/prism.css" rel="stylesheet" />
                <link href="css/app.css" rel="stylesheet" />
            </head>
            <body>
                <div id="app">
                    <article style="max-width:800px;margin:2rem auto;padding:0 1rem;">
                        <h1>{title}</h1>
                        <p class="lead text-muted">{description}</p>
                        {entry.HtmlContent}
                    </article>
                </div>
                <div id="blazor-error-ui">
                    An unhandled error has occurred.
                    <a href="" class="reload">Reload</a>
                    <a class="dismiss">X</a>
                </div>
                <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"
                        integrity="sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz" crossorigin="anonymous"></script>
                <script src="lib/prismjs/prism.js"></script>
                <script src="js/docs.js"></script>
                <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """;
    }

    private string BuildJsonLd(ContentIndexEntry entry)
    {
        var url = $"{_baseUrl}/docs/{entry.Slug}";
        var slugParts = entry.Slug.Split('/');
        var breadcrumbs = new List<object>
        {
            new { @type = "ListItem", position = 1, name = "Docs", item = $"{_baseUrl}/" }
        };

        if (slugParts.Length > 1)
        {
            breadcrumbs.Add(new { @type = "ListItem", position = 2, name = entry.Category, item = $"{_baseUrl}/docs/{slugParts[0]}" });
            breadcrumbs.Add(new { @type = "ListItem", position = 3, name = entry.Title, item = url });
        }
        else
        {
            breadcrumbs.Add(new { @type = "ListItem", position = 2, name = entry.Title, item = url });
        }

        var graph = new object[]
        {
            new
            {
                @context = "https://schema.org",
                @type = "TechArticle",
                headline = entry.Title,
                description = entry.Description,
                url,
                publisher = new { @type = "Organization", name = "Commerzbank.NET" }
            },
            new
            {
                @context = "https://schema.org",
                @type = "BreadcrumbList",
                itemListElement = breadcrumbs
            }
        };

        return JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task Generate404(string wwwrootPath)
    {
        var html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Page Not Found - Commerzbank.NET Docs</title>
                <base href="/" />
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                      integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />
                <link href="css/app.css" rel="stylesheet" />
                <script>
                    // GitHub Pages SPA redirect
                    sessionStorage.redirect = location.href;
                </script>
                <meta http-equiv="refresh" content="0;URL='/'">
            </head>
            <body>
                <div id="app"></div>
            </body>
            </html>
            """;

        await File.WriteAllTextAsync(Path.Combine(wwwrootPath, "404.html"), html).ConfigureAwait(false);
        Console.WriteLine("  Generated 404.html");
    }

    private async Task InjectMetaTags(string wwwrootPath)
    {
        const string metaTags = """

                <meta name="description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API: message download, order submission, and ISO 20022 camt/pain reading and writing." />
                <meta property="og:type" content="website" />
                <meta property="og:title" content="Commerzbank.NET Docs" />
                <meta property="og:description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API: message download, order submission, and ISO 20022 camt/pain reading and writing." />
                <meta property="og:site_name" content="Commerzbank.NET Docs" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="Commerzbank.NET Docs" />
                <meta name="twitter:description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API." />
            """;

        foreach (var fileName in new[] { "index.html", "404.html" })
        {
            var filePath = Path.Combine(wwwrootPath, fileName);
            if (!File.Exists(filePath)) continue;

            var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

            if (content.Contains("og:title", StringComparison.Ordinal)) continue;

            const string marker = """<meta name="viewport" content="width=device-width, initial-scale=1.0" />""";
            content = content.Replace(marker, marker + metaTags);

            await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);
            Console.WriteLine($"  Injected meta tags into {fileName}");
        }
    }
}
