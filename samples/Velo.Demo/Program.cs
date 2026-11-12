using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Velo.Blazor;
using Velo.Demo;
using Velo.Demo.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddVelo(o => o.Texts = VeloTexts.French); // the demo site is in French
builder.Services.AddScoped<DemoThemeState>();
builder.Services.AddScoped<DemoCodeState>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<EmployeeApi>();

await builder.Build().RunAsync();
