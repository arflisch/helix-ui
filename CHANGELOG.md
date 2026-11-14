# Changelog

All notable changes to the Helix UI packages. The packages share one version number.
Documentation and live examples: [helix-ui-docs.pages.dev](https://helix-ui-docs.pages.dev/).

## 0.4.0

The first release since 0.1.1. Versions 0.2.0 and 0.3.0 were never published to nuget.org, so their changes are included here.

### New packages

- **Helix.Avalonia**: the Helix controls for Avalonia 12 (Windows, macOS, Linux), with the same tokens, light and dark
  themes and components as WPF. Includes:
  - data grid, charts, donut, gauge, heatmap, bar list
  - fields and pickers
  - timeline, Kanban, sortable list, dashboard
  - dialogs, drawers, confirm and prompt, navigation and menus
  - time bar, notification center, filter bar, color picker, file drop
  - tree view, carousel, log viewer, auto form, description list, target bar, busy veil
- **Helix.Export**: Excel (.xlsx), Word (.docx) and CSV files from your lists, with no dependency and no Office needed.
  - Cells are typed; numbers, dates and totals keep their formats.
  - The header is frozen and filterable; Word files get headings, tables and page numbers.
  - `HelixExcelStyle` gives presets (Default, Bold, Minimal, Grid) plus header, font, borders, stripes, totals,
    negatives and cell rules.
- **Helix.Templates**: `dotnet new helix-blazor` and `dotnet new helix-wpf` create an app with the Helix theme, a layout
  and a ready-made report page (filters, grid, export).

### New components (Blazor, WPF and Avalonia)

- `HelixKanbanBoard`: a Kanban board that edits its own cards.
  - **+** adds a card. A click, Enter, E or F2 opens the editor: title, description, column, due date, assignee, tags,
    color.
  - Delete asks for confirmation, and overdue dates turn red.
  - Comes with `HelixKanbanCard`, whose `Data` holds your own object, and events when a card is added, changed or
    deleted.
- `HelixNotificationCenter`: a bell with the unread count.
  - Notifications are grouped by day, with severity, relative time, a link or action, dismiss and mark all read.
- `HelixTimeBar`: a live countdown to a repeating or fixed deadline, with slot chips and their statuses.
- `HelixFilterBar`: text, choice, date and toggle filters, saved filters, and `HelixDataGrid.Filter`.
- `HelixAutoForm`: a form built from a model and its DataAnnotations, with two-way bindings and validation.
- `HelixLogViewer`, `HelixBusy`, `HelixTargetBar`, `HelixDescriptionList`.
- `HelixCalendar`, `HelixDateTimePicker`, `HelixDateRangePicker` (with presets), `HelixMultiSelect`.

### Improvements

- **Log viewer**
  - Entries carry `Properties`, `Exception`, `TraceId` and `User`, shown in a details panel you can copy.
  - A click on a value adds a filter chip. You can also filter by source and by period.
  - A "N new lines" button appears while not following.
  - The lines shown export to CSV or Excel.
- **Data grid**
  - Row details, groups with subtotals, a totals row.
  - Badge, signed and unit columns.
  - Excel, Word and CSV export; `ExcelStyle`.
- **Select**: searchable, with local or server search and an item template.
- **NumberField**: signed and calculation modes.
- **Dialogs**: `PromptAsync`, and `ShowAsync` to put any view or component in a dialog.
- **Async work**: `HelixButton.AutoLoading` (Blazor) and `HelixAsyncCommand` (WPF), with busy state, errors as toasts
  and cancel.
- **User settings**: `RememberAs` on the dashboard, data grid, filter bar and splitter.
- **Live collections**: batched updates from any thread, and time slots that handle daylight saving.
- **Dashboard**: a columns layout with drag and drop.
- **WPF**: `HelixResources.Follow` takes the colors of the app theme (a MaterialDesign map or your own keys).

### Fixes

- Avalonia grid columns bind one way, which fixes a stack overflow with English formats.
- The stat sparkline no longer overflows on narrow tiles.

## 0.1.1

- Packages link to the documentation site.

## 0.1.0

- First release of Helix.Blazor and Helix.Wpf: theme with light and dark palettes, fields, buttons, data grid, charts,
  donut, tree view, carousel, timeline, Kanban, dashboard and more.
