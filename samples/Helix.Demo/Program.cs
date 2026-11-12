using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Helix.Blazor;
using Helix.Demo;
using Helix.Demo.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHelix(o => o.Texts = HelixTexts.English); // the demo site is in English
builder.Services.AddScoped<DemoThemeState>();
builder.Services.AddScoped<DemoCodeState>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<EmployeeApi>();

await builder.Build().RunAsync();
