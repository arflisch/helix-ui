# Helix.Export

Excel (.xlsx), Word (.docx) and CSV files from your lists, in a few lines. No dependency, no Office needed: plain .NET 8 / 10,
so it runs the same in Blazor, WPF, an ASP.NET Core API or a background job.

```bash
dotnet add package Helix.Export
```

## One line

```csharp
using Helix.Export;

byte[] xlsx = HelixExport.ToExcel(employees);                    // every public property
byte[] docx = HelixExport.ToWord(employees, title: "Employees");
string csv  = HelixExport.ToCsv(employees, c => c
    .Add("Name", e => e.Name)
    .Add("Salary", e => e.Salary, "N0", HelixTotal.Sum));
```

Without columns, every public property becomes a column; `[Display(Name, Order)]`, `[DisplayFormat]`,
`[Browsable(false)]` and `[Display(AutoGenerateField = false)]` are honoured.

## Excel

```csharp
new HelixExcel()
    .Sheet("Employees", employees, c => c
        .Add("Name", e => e.Name)
        .Add("Salary", e => e.Salary, "N0", HelixTotal.Sum)
        .Add("Start", e => e.StartDate, "dd/MM/yyyy")
        .Add("Load", e => e.Load, "P0", HelixTotal.Average), title: "Employees")
    .Sheet("Summary", summary)
    .Save("employees.xlsx");          // or ToBytes(), WriteTo(stream)
```

- Numbers, dates, durations and booleans are real Excel values (sortable, summable).
- .NET formats are translated (`N2` → `#,##0.00`, `P0` → `0%`, `dd/MM/yyyy HH:mm` → `dd/mm/yyyy hh:mm`), or pass `excelFormat`.
- Header frozen and filterable, column widths from the content, totals as formulas (`SUM`, `AVERAGE`…).

## Word

```csharp
new HelixWord { PageNumbers = true, Landscape = false, HeaderText = "Monthly report" }
    .Title("Team report")
    .Paragraph("Figures for November.")
    .Details(("Site", "North"), ("Period", "November 2026"))
    .Heading("Highlights")
    .Bullets(["Largest team: Engineering", "3 on leave"])
    .Heading("Employees", 2)
    .Table(employees, c => c.Add("Name", e => e.Name).Add("Salary", e => e.Salary, "N0", HelixTotal.Sum))
    .PageBreak()
    .Save("report.docx");
```

Tables repeat their header on each page, align numbers on the right and can end with a totals row.

## With Helix UI grids

`HelixDataGrid` (Blazor and WPF) uses this package: `ExportFormats="All"` offers Excel, Word and CSV on its Export button,
and `grid.ToExportTable()` hands the visible columns and filtered rows to your own report.

Docs: https://helix-ui-docs.pages.dev/export
