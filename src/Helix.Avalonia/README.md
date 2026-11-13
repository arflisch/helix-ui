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

## Fields and pickers

```xml
<helix:HelixNumberField Label="Capacity" Value="{Binding Capacity}" Unit="MW" Minimum="0" Step="5" AllowExpressions="True" />
<helix:HelixSelect Label="Region" ItemsSource="{Binding Regions}" SelectedItem="{Binding Region}" IsSearchable="True" />
<helix:HelixMultiSelect Label="Teams" ItemsSource="{Binding Teams}" SelectedItems="{Binding Chosen}" MaxChips="2" />
<helix:HelixDatePicker Label="Delivery" SelectedDate="{Binding Delivery}" Minimum="{Binding Today}" />
<helix:HelixDateTimePicker Label="Start" SelectedDateTime="{Binding Start}" MinuteStep="15" />
<helix:HelixDateRangePicker Label="Period" SelectedRange="{Binding Period}" MaxDays="31" />
<helix:HelixRadioGroup Label="Plan" ItemsSource="{Binding Plans}" Appearance="Cards" />   <!-- Default, Cards, Segmented -->
<helix:HelixSlider Label="Volume" Value="{Binding Volume}" Maximum="100" Unit="%" />
<helix:HelixRating Value="{Binding Stars}" ShowValue="True" />
```

`IsDateDisabled` (e.g. weekends) on the date pickers and on `HelixCalendar`, which can also be used on its own.

## Planning and boards

```xml
<helix:HelixTimeline Title="Schedule" Start="{Binding From}" End="{Binding To}" SlotDuration="0:30:00"
                     Items="{Binding Items}" Statuses="{Binding Statuses}" IsEditable="True" ItemChanged="OnItemChanged" />
<helix:HelixKanban Columns="{Binding Columns}" ItemsSource="{Binding Cards}" ColumnPath="Status" />
<helix:HelixSortableList ItemsSource="{Binding Priorities}" />
<helix:HelixDashboard Title="Operations" ColumnCount="3" Layout="{Binding SavedLayout}">
    <helix:HelixDashboardTile TileId="load" Title="Load" DefaultSpan="2">…</helix:HelixDashboardTile>
</helix:HelixDashboard>
```

Dragging works with the mouse, touch and pen (pointer capture, no system drag and drop), the same on every platform:
timeline items (move, stretch, change lane), kanban cards (with a placeholder and a copy following the pointer),
sortable rows (and Alt + ↑ / ↓), dashboard tiles in edit mode (plus move, resize and hide buttons).

## Controls so far

HelixButton, HelixIcon, HelixBadge, HelixAlert, HelixCard, HelixStat, HelixProgress, HelixSpinner, HelixEmptyState,
HelixTextField, HelixSwitch, HelixCheckBox, HelixToasts / HelixToastHost, HelixAsyncCommand,
HelixDataGrid, HelixChart, HelixDonut, HelixGauge, HelixHeatmap, HelixBarList,
HelixNumberField, HelixSelect, HelixMultiSelect, HelixCalendar, HelixDatePicker, HelixDateTimePicker, HelixDateRangePicker,
HelixRadioGroup, HelixSlider, HelixRating,
HelixTimeline, HelixKanban, HelixSortableList, HelixDashboard (tiles, Rows or Columns layout).
More are being ported from Helix.Wpf.

Gallery: `dotnet run --project samples/Helix.Avalonia.Demo`
(`-- --screenshots <folder>` renders every page in light and dark to PNG; `-- --selftest` checks the grid, the pickers and the drags (24 checks)).
