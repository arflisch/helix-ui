# Velo.Wpf

Clean, modern WPF controls that look good by default and are easy to customize:
every visual part is a property (`ThumbBrush`, `TrackBrush`, `CornerRadius`…), so you never copy a template.

## Install

```bash
dotnet add package Velo.Wpf
```

```xml
<!-- App.xaml: the theme (light, dark or following Windows) -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <velo:VeloResources Mode="System" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>

<!-- MainWindow.xaml: xmlns:velo="https://velo-ui.dev/wpf" -->
<velo:VeloHost>   <!-- toasts, dialogs and confirmations -->
    …your layout…
</velo:VeloHost>
```

## Use

```xml
<velo:VeloButton Content="Save" Icon="check" Click="OnSave" />
<velo:VeloSwitch Content="Autopilot" ThumbBrush="#F59E0B" OnText="ON" OffText="OFF" IsChecked="{Binding Auto}" />
<velo:VeloTextField Label="E-mail" Icon="mail" Text="{Binding Email}" Error="{Binding EmailError}" />
```

```csharp
VeloToasts.Success("Saved.");
if (await VeloConfirm.AskAsync("Delete?", "This cannot be undone.", danger: true)) { … }
```

## What's inside

Buttons, menus, text / number / select / date / color fields, switch, checkbox, radio group, slider, rating,
data grid (client or server), chart, donut, gauge, heatmap, bar list, timeline, kanban, sortable list, dashboard,
tree view (list or org chart), tabs, accordion, stepper, pagination, carousel, dialog, drawer, toasts, confirmations.

- Light, dark or Windows theme, with your own palettes (`VeloResources`).
- Change a default app-wide with a `BasedOn` style: the Velo template stays.
- Built-in texts in English and French (`VeloTexts`).

Requires .NET 10 on Windows. MIT license.
