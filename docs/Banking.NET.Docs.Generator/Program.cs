using Banking.NET.Docs.Generator;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: Banking.NET.Docs.Generator <wwwroot-path> [--base-url <url>] [--base-path <path>]");
    return 1;
}

var wwwrootPath = args[0];
if (!Directory.Exists(wwwrootPath))
{
    Console.Error.WriteLine($"wwwroot path not found: {wwwrootPath}");
    return 1;
}

var origin = "https://emuuu.github.io";
var baseUrlIndex = Array.IndexOf(args, "--base-url");
if (baseUrlIndex >= 0 && baseUrlIndex + 1 < args.Length)
{
    origin = args[baseUrlIndex + 1];
}
origin = origin.TrimEnd('/');

var basePath = "/";
var basePathIndex = Array.IndexOf(args, "--base-path");
if (basePathIndex >= 0 && basePathIndex + 1 < args.Length)
{
    basePath = args[basePathIndex + 1];
}
basePath = NormalizeBasePath(basePath);

// The canonical site root the sitemap, canonical links and JSON-LD point to: the origin combined with
// the deployment base path, so the two are never maintained as two independently drifting defaults.
var siteBaseUrl = basePath == "/" ? origin : $"{origin}{basePath.TrimEnd('/')}";

var dataDir = Path.Combine(wwwrootPath, "data");
Directory.CreateDirectory(dataDir);

Console.WriteLine("Generating REST API documentation...");
var restApiGenerator = new RestApiDocGenerator();
var apiDocs = await restApiGenerator.GenerateAsync(Path.Combine(dataDir, "rest-api-docs.json"));
var apiSlugs = apiDocs.Types.Select(t => t.Slug).Concat(apiDocs.Enums.Select(e => e.Slug)).ToList();

Console.WriteLine("Generating content index...");
var contentGenerator = new ContentIndexGenerator();
var entries = await contentGenerator.GenerateAsync(
    Path.Combine(wwwrootPath, "content"),
    Path.Combine(dataDir, "content-index.json"));

Console.WriteLine("Generating sitemap and robots.txt...");
var sitemapGenerator = new SitemapGenerator(siteBaseUrl);
await sitemapGenerator.GenerateAsync(wwwrootPath, entries, apiSlugs);

Console.WriteLine("Generating static HTML pages...");
var staticHtmlGenerator = new StaticHtmlGenerator(siteBaseUrl, basePath);
await staticHtmlGenerator.GenerateAsync(wwwrootPath, entries);

Console.WriteLine("Documentation data generated successfully.");
return 0;

static string NormalizeBasePath(string basePath)
{
    if (string.IsNullOrWhiteSpace(basePath)) return "/";

    var normalized = basePath.Trim();
    if (!normalized.StartsWith('/')) normalized = "/" + normalized;
    if (!normalized.EndsWith('/')) normalized += "/";
    while (normalized.Contains("//", StringComparison.Ordinal))
        normalized = normalized.Replace("//", "/");

    return normalized;
}
