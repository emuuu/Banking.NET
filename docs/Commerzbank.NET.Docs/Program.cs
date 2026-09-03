using Commerzbank.NET.Docs;
using Commerzbank.NET.Docs.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Mutable sandbox credentials entered by the visitor (Singleton for WASM)
builder.Services.AddSingleton<DocsCredentialService>();

// Builds a fresh ICorporatePaymentsClient whenever the credentials change, since the auth handler
// chain captures the client ID/secret at construction time and cannot be reconfigured afterwards.
builder.Services.AddSingleton<CorporatePaymentsClientFactory>();

// Doc services
builder.Services.AddScoped<IRestApiDocService, RestApiDocService>();
builder.Services.AddScoped<IDocContentService, DocContentService>();

await builder.Build().RunAsync();
