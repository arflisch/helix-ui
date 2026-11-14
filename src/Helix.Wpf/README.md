# Helix.Wpf

Clean, modern WPF controls that look good by default and are easy to customize:
every visual part is a property (`ThumbBrush`, `TrackBrush`, `CornerRadius`…), so you never copy a template.

**Documentation and examples (switch the code blocks to WPF): [helix-ui-docs.pages.dev](https://helix-ui-docs.pages.dev/)**

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

- **Fields**: text, number (signed, calculations), select and multi-select (searchable), date, date-time and
  date range, color, switch, checkbox, radio group, slider, rating, auto form built from a model
  (two-way bindings, DataAnnotations validation).
- **Data**: data grid (client or server; groups with subtotals, totals row, row details, column chooser,
  Excel / Word / CSV export with `ExcelStyle`), filter bar with saved filters, description list.
- **Charts**: chart, donut, gauge, heatmap, bar list, stat with change, target bar.
- **Planning and boards**: timeline (lanes, statuses, drag), time bar (live countdown to a deadline),
  Kanban, editable Kanban board, sortable list, dashboard (tiles, columns layout, edit mode, saved layout).
- **Live**: log viewer (levels, search, source and period filters, details with properties, exception and
  trace id, filter chips, CSV / Excel export), notification center, busy veil, live collections, `HelixAsyncCommand`.
- **Layout and navigation**: tabs, accordion, stepper, pagination, menu button and context menu, tree view (list or org chart), carousel.
- **Overlays**: dialogs (any view in a dialog), drawer, toasts, confirm and prompt.

```xml
<!-- A Kanban that adds, edits and deletes its cards itself (ObservableCollection<HelixKanbanCard>) -->
<helix:HelixKanbanBoard Columns="{Binding Columns}" ItemsSource="{Binding Cards}" CardChanged="OnSaved" />

<!-- A log with details, filters and export (ObservableCollection<HelixLogEntry>, followed live) -->
<helix:HelixLogViewer ItemsSource="{Binding Log}" Height="480" ExportFileName="operations-log" />
```

- Light, dark or Windows theme, with your own palettes (`HelixResources`), or the colors of your app theme
  (`HelixResources.Follow`).
- Change a default app-wide with a `BasedOn` style: the Helix template stays.
- Built-in texts in English and French (`HelixTexts`).
- Excel, Word and CSV files come from [Helix.Export](https://www.nuget.org/packages/Helix.Export), included.

Requires .NET 8 or later on Windows. MIT license.
