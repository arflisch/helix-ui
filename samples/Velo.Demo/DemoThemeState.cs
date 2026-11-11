using Velo.Blazor;

namespace Velo.Demo;

/// <summary>Theme shared by the whole demo, edited from the top bar and the customization page.</summary>
public sealed class DemoThemeState
{
    public VeloTheme Theme { get; } = new();

    public event Action? Changed;

    public void Notify() => Changed?.Invoke();
}
