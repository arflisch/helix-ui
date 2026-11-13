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

## Data grid

```xml
<helix:HelixDataGrid Source="{Binding Employees}" IsSearchable="True" ShowColumnChooser="True"
                     IsExportable="True" ExportFormats="All" ExportTitle="Employees" PageSize="10">
    <helix:HelixDataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*" />
        <DataGridTextColumn Header="Salary" Binding="{Binding Salary, StringFormat={}{0:N0}}"
                            helix:HelixDataGrid.Aggregate="Sum" helix:HelixDataGrid.Unit="€" />
    </helix:HelixDataGrid.Columns>
</helix:HelixDataGrid>
```

Search, sort across pages, paging, column chooser, density, `GroupBy`, totals row (`Aggregate`, `Unit`),
export to Excel / Word / CSV (`ExcelStyle` for the look), server mode with `ItemsProvider`. Columns are ordinary
Avalonia DataGrid columns (the DataGrid theme is included by HelixTheme).

## Charts

```xml
<helix:HelixChart Title="Load" Unit="MW" ShowNow="True">
    <helix:HelixChartSeries Label="Measured" Type="Area" Points="{Binding Measured}" />
    <helix:HelixChartSeries Label="Forecast" IsDashed="True" Points="{Binding Forecast}" />
    <helix:HelixChart.Lines><helix:HelixChartLine Y="520" Label="Limit" Color="danger" /></helix:HelixChart.Lines>
</helix:HelixChart>
```

HelixChart (line, area, step, bar), HelixDonut (or segmented bar), HelixGauge, HelixHeatmap, HelixBarList.
Note: a series is named with `Label` (in Avalonia, `Name` is the element's x:Name).

## Controls so far

HelixButton, HelixIcon, HelixBadge, HelixAlert, HelixCard, HelixStat, HelixProgress, HelixSpinner, HelixEmptyState,
HelixTextField, HelixSwitch, HelixCheckBox, HelixToasts / HelixToastHost, HelixAsyncCommand,
HelixDataGrid, HelixChart, HelixDonut, HelixGauge, HelixHeatmap, HelixBarList.
More are being ported from Helix.Wpf.

Gallery: `dotnet run --project samples/Helix.Avalonia.Demo`
(`-- --screenshots <folder>` renders every page in light and dark to PNG; `-- --selftest` checks the grid behavior).
