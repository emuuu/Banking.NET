using Banking.NET.Docs;
using Banking.NET.Docs.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Doc services
builder.Services.AddScoped<IRestApiDocService, RestApiDocService>();
builder.Services.AddScoped<IDocContentService, DocContentService>();

await builder.Build().RunAsync();
