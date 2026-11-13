# Helix.Avalonia

Helix UI for Avalonia 12: the same look, tokens and property names as Helix for Blazor and WPF,
on Windows, macOS and Linux.

```bash
dotnet add package Helix.Avalonia
```

```xml
<!-- App.axaml -->
<Application.Styles>
    <FluentTheme />
    <helix:HelixTheme Mode="System" />   <!-- Light, Dark or System -->
</Application.Styles>
```

```xml
<helix:HelixButton Content="Save" Icon="check" />
<helix:HelixButton Content="Delete" Tone="Danger" Variant="Soft" />
<helix:HelixTextField Label="Email" Icon="mail" IsClearable="True" Hint="We never share it." />
<helix:HelixAlert Severity="Warning" Title="Deadline in 5 minutes">Submit before 14:10.</helix:HelixAlert>
```

```csharp
HelixToasts.Success("Saved");          // needs <helix:HelixToastHost /> over the window content
HelixTheme.Current?.Toggle();          // light / dark
```

Brushes are `{DynamicResource Helix.Primary}`, `Helix.Surface`, `Helix.Text`… in light and dark (ThemeDictionaries).
Change colors with `<helix:HelixTheme.Light><helix:HelixPalette Primary="#c2410c" /></helix:HelixTheme.Light>`.

## Controls so far

HelixButton, HelixIcon, HelixBadge, HelixAlert, HelixCard, HelixStat, HelixProgress, HelixSpinner, HelixEmptyState,
HelixTextField, HelixSwitch, HelixCheckBox, HelixToasts / HelixToastHost, HelixAsyncCommand.
More are being ported from Helix.Wpf.

Gallery: `dotnet run --project samples/Helix.Avalonia.Demo`
(`-- --screenshots <folder>` renders every page in light and dark to PNG).
