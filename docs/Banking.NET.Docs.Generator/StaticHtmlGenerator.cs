using System.Text.Json;
using System.Web;

namespace Banking.NET.Docs.Generator;

/// <summary>
/// Pre-renders a static <c>index.html</c> per content page (for SEO and direct/crawler navigation) and the
/// site's <c>404.html</c> SPA fallback, both carrying the deployment's base path and shared meta tags. Does
/// not touch the checked-in <c>wwwroot/index.html</c> shell - that file stays source-controlled with a
/// fixed <c>&lt;base href="/" /&gt;</c>, and the deployment base path is applied to the published copy only.
/// </summary>
public class StaticHtmlGenerator
{
    private readonly string _baseUrl;
    private readonly string _basePath;

    /// <param name="baseUrl">The site's absolute base URL, used for canonical links and JSON-LD.</param>
    /// <param name="basePath">The <c>&lt;base href&gt;</c> to write into every generated page (e.g. <c>/</c> locally, <c>/Banking.NET/</c> on GitHub Pages).</param>
    public StaticHtmlGenerator(string baseUrl, string basePath)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _basePath = basePath;
    }

    /// <summary>Generates one static page per content entry under <c>docs/{slug}/index.html</c>, then <c>404.html</c>.</summary>
    /// <param name="wwwrootPath">The Blazor app's wwwroot directory.</param>
    /// <param name="entries">The generated content index entries to render pages for.</param>
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
                <title>{title} - Banking.NET Docs</title>
                <base href="{_basePath}" />
                <meta name="description" content="{description}" />
                <link rel="canonical" href="{url}" />
                <meta property="og:type" content="article" />
                <meta property="og:title" content="{title} - Banking.NET" />
                <meta property="og:description" content="{description}" />
                <meta property="og:url" content="{url}" />
                <meta property="og:site_name" content="Banking.NET Docs" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="{title} - Banking.NET" />
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
                publisher = new { @type = "Organization", name = "Banking.NET" }
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
        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API: message download, order submission, and ISO 20022 camt/pain reading and writing." />
                <meta property="og:type" content="website" />
                <meta property="og:title" content="Banking.NET Docs" />
                <meta property="og:description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API: message download, order submission, and ISO 20022 camt/pain reading and writing." />
                <meta property="og:site_name" content="Banking.NET Docs" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="Banking.NET Docs" />
                <meta name="twitter:description" content="An unofficial .NET client library for the Commerzbank Corporate Payments API." />
                <title>Page Not Found - Banking.NET Docs</title>
                <base href="{{_basePath}}" />
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                      integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />
                <link href="css/app.css" rel="stylesheet" />
                <script>
                    // GitHub Pages SPA fallback: stash the requested path so index.html can restore it
                    // via history.replaceState once the Blazor router is ready (see wwwroot/index.html).
                    sessionStorage.redirect = location.href;
                </script>
                <meta http-equiv="refresh" content="0;URL='{{_basePath}}'">
            </head>
            <body>
                <div id="app"></div>
            </body>
            </html>
            """;

        await File.WriteAllTextAsync(Path.Combine(wwwrootPath, "404.html"), html).ConfigureAwait(false);
        Console.WriteLine("  Generated 404.html");
    }
}
