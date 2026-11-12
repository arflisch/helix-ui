using Helix.Blazor;
using HelixApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Toasts, confirmations, dialogs and remembered user settings (in the browser by default).
builder.Services.AddHelix(o =>
{
    o.Defaults.Button.AutoLoading = true;   // every button shows its spinner while its OnClick runs
});

await builder.Build().RunAsync();
