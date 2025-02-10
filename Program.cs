using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAiChatToolBlazorDemo;
using OpenAiChatToolBlazorDemo.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Configuration.AddJsonFile("appsettings.json");

// Bind configuration to ModelSettings

var foo = builder.Configuration.GetSection("ModelSettings");

var modelSettings = new ModelSettings();
builder.Configuration.Bind("ModelSettings", modelSettings);
builder.Services.AddSingleton(modelSettings);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp =>
{
    var modelSettings = sp.GetRequiredService<ModelSettings>();
    return new OpenAIClient(modelSettings.OPENAI_API_KEY);
});

await builder.Build().RunAsync();
