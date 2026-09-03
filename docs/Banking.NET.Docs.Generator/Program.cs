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

var baseUrl = "https://emuuu.github.io/Banking.NET";
var baseUrlIndex = Array.IndexOf(args, "--base-url");
if (baseUrlIndex >= 0 && baseUrlIndex + 1 < args.Length)
{
    baseUrl = args[baseUrlIndex + 1];
}

var basePath = "/";
var basePathIndex = Array.IndexOf(args, "--base-path");
if (basePathIndex >= 0 && basePathIndex + 1 < args.Length)
{
    basePath = args[basePathIndex + 1];
}

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
var sitemapGenerator = new SitemapGenerator(baseUrl);
await sitemapGenerator.GenerateAsync(wwwrootPath, entries, apiSlugs);

Console.WriteLine("Generating static HTML pages...");
var staticHtmlGenerator = new StaticHtmlGenerator(baseUrl, basePath);
await staticHtmlGenerator.GenerateAsync(wwwrootPath, entries);

Console.WriteLine("Documentation data generated successfully.");
return 0;
