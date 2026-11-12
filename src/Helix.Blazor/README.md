# Helix.Blazor

Clean, modern Blazor components that look good by default and are easy to customize:
every visual part is a parameter (`ThumbColor`, `TrackColor`, `Radius`…), so you never copy a template.

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

Buttons, menus, text / number / select / date / color fields, switch, checkbox, radio group, slider, rating,
data grid (client or server), chart, donut, gauge, heatmap, bar list, timeline, kanban, sortable list, dashboard,
tree view, tabs, accordion, stepper, pagination, carousel, dialog, drawer, toasts, confirmations, and more.

- Light, dark or system theme, with your own palettes (`HelixTheme`).
- App-wide defaults for every component (`AddHelix(o => o.Defaults…)`).
- Built-in texts in English and French, replaceable (`HelixTexts`).

MIT license.
