namespace Velo.Demo;

/// <summary>Which version of the code samples is shown (Blazor or WPF). One choice for the whole site.</summary>
public sealed class DemoCodeState
{
    public bool ShowWpf { get; private set; }

    public event Action? Changed;

    public void Set(bool wpf)
    {
        if (ShowWpf == wpf) return;
        ShowWpf = wpf;
        Changed?.Invoke();
    }
}
