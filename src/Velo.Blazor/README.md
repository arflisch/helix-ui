# Velo.Blazor

Clean, modern Blazor components that look good by default and are easy to customize:
every visual part is a parameter (`ThumbColor`, `TrackColor`, `Radius`…), so you never copy a template.

## Install

```bash
dotnet add package Velo.Blazor
```

```csharp
// Program.cs
builder.Services.AddVelo();
```

```razor
@* App.razor, in <head> *@
<link rel="stylesheet" href="_content/Velo.Blazor/velo.css" />

@* _Imports.razor *@
@using Velo.Blazor

@* MainLayout.razor: theme, dark mode, toasts and confirmations *@
<VeloProvider Theme="theme">@Body</VeloProvider>
```

## Use

```razor
<VeloButton Icon="check" OnClick="Save">Save</VeloButton>
<VeloSwitch @bind-Checked="autopilot" Label="Autopilot" ThumbColor="#F59E0B" OnText="ON" OffText="OFF" />
<VeloTextField Label="E-mail" Icon="mail" @bind-Value="email" Error="@emailError" />
```

## What's inside

Buttons, menus, text / number / select / date / color fields, switch, checkbox, radio group, slider, rating,
data grid (client or server), chart, donut, gauge, heatmap, bar list, timeline, kanban, sortable list, dashboard,
tree view, tabs, accordion, stepper, pagination, carousel, dialog, drawer, toasts, confirmations, and more.

- Light, dark or system theme, with your own palettes (`VeloTheme`).
- App-wide defaults for every component (`AddVelo(o => o.Defaults…)`).
- Built-in texts in English and French, replaceable (`VeloTexts`).

MIT license.
