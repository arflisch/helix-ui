# Helix.Wpf

Clean, modern WPF controls that look good by default and are easy to customize:
every visual part is a property (`ThumbBrush`, `TrackBrush`, `CornerRadius`…), so you never copy a template.

## Install

```bash
dotnet add package Helix.Wpf
```

```xml
<!-- App.xaml: the theme (light, dark or following Windows) -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <helix:HelixResources Mode="System" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>

<!-- MainWindow.xaml: xmlns:helix="https://helix-ui.dev/wpf" -->
<helix:HelixHost>   <!-- toasts, dialogs and confirmations -->
    …your layout…
</helix:HelixHost>
```

## Use

```xml
<helix:HelixButton Content="Save" Icon="check" Click="OnSave" />
<helix:HelixSwitch Content="Autopilot" ThumbBrush="#F59E0B" OnText="ON" OffText="OFF" IsChecked="{Binding Auto}" />
<helix:HelixTextField Label="E-mail" Icon="mail" Text="{Binding Email}" Error="{Binding EmailError}" />
```

```csharp
HelixToasts.Success("Saved.");
if (await HelixConfirm.AskAsync("Delete?", "This cannot be undone.", danger: true)) { … }
```

## What's inside

Buttons, menus, text / number / select / date / color fields, switch, checkbox, radio group, slider, rating,
data grid (client or server), chart, donut, gauge, heatmap, bar list, timeline, kanban, sortable list, dashboard,
tree view (list or org chart), tabs, accordion, stepper, pagination, carousel, dialog, drawer, toasts, confirmations.

- Light, dark or Windows theme, with your own palettes (`HelixResources`).
- Change a default app-wide with a `BasedOn` style: the Helix template stays.
- Built-in texts in English and French (`HelixTexts`).

Requires .NET 10 on Windows. MIT license.
