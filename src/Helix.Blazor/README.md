# Helix.Blazor

Clean, modern Blazor components that look good by default and are easy to customize:
every visual part is a parameter (`ThumbColor`, `TrackColor`, `Radius`…), so you never copy a template.

**Documentation and live examples: [helix-ui-docs.pages.dev](https://helix-ui-docs.pages.dev/)**

## Install

```bash
dotnet add package Helix.Blazor
```

```csharp
// Program.cs
builder.Services.AddHelix();
```

```razor
@* App.razor, in <head> *@
<link rel="stylesheet" href="_content/Helix.Blazor/helix.css" />

@* _Imports.razor *@
@using Helix.Blazor

@* MainLayout.razor: theme, dark mode, toasts and confirmations *@
<HelixProvider Theme="theme">@Body</HelixProvider>
```

## Use

```razor
<HelixButton Icon="check" OnClick="Save">Save</HelixButton>
<HelixSwitch @bind-Checked="autopilot" Label="Autopilot" ThumbColor="#F59E0B" OnText="ON" OffText="OFF" />
<HelixTextField Label="E-mail" Icon="mail" @bind-Value="email" Error="@emailError" />
```

## What's inside

- **Fields**: text, number (signed, calculations), select and multi-select (searchable, local or server),
  date, date-time and date range (with presets), calendar, color, switch, checkbox, radio group, slider, rating,
  auto form built from a model and its DataAnnotations.
- **Data**: data grid (client or server; groups with subtotals, totals row, row details, column chooser,
  Excel / Word / CSV export with `ExcelStyle`), filter bar with saved filters, description list.
- **Charts**: chart, donut, gauge, heatmap, bar list, stat with sparkline, target bar.
- **Planning and boards**: timeline (lanes, statuses, drag), time bar (live countdown to a deadline),
  Kanban, editable Kanban board, sortable list, dashboard (tiles, edit mode, saved layout).
- **Live**: log viewer (levels, search, source and period filters, details with properties, exception and
  trace id, filter chips, CSV / Excel export), notification center, busy veil, live collections.
- **Layout and navigation**: tabs, accordion, stepper, pagination, menu, tree view, carousel, splitter.
- **Overlays**: dialogs (your component in a dialog), drawer, toasts, confirm and prompt.

```razor
@* A Kanban that adds, edits and deletes its cards itself *@
<HelixKanbanBoard Columns="columns" @bind-Cards="cards" OnCardChanged="Save" />

@* A log with details, filters and export *@
<HelixLogViewer Entries="entries" Height="480px" ExportFileName="operations-log" />
```

- Light, dark or system theme, with your own palettes (`HelixTheme`).
- App-wide defaults for every component (`AddHelix(o => o.Defaults…)`); preferences remembered per user (`RememberAs`).
- Built-in texts in English and French, replaceable (`HelixTexts`).
- Excel, Word and CSV files come from [Helix.Export](https://www.nuget.org/packages/Helix.Export), included.

MIT license.
