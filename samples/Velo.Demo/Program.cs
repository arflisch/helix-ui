using Velo.Blazor;
using Velo.Demo;
using Velo.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddVelo(o => o.Texts = VeloTexts.French); // the demo site is in French
builder.Services.AddScoped<DemoThemeState>();
builder.Services.AddScoped<DemoCodeState>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<EmployeeApi>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
