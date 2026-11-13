using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace Helix.Avalonia;

/// <summary>Where controls with RememberAs keep their settings. Text values; implement it for a database or an API, per user.</summary>
public interface IHelixSettingsStore
{
    string? Get(string key);

    /// <summary>Saves a value; null removes it.</summary>
    void Set(string key, string? value);
}

/// <summary>The default store: one JSON file per user, in the local application data folder (&lt;application&gt;/helix-settings.json).</summary>
public sealed class HelixFileSettingsStore : IHelixSettingsStore
{
    private readonly string _path;
    private Dictionary<string, string>? _values;

    public HelixFileSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Assembly.GetEntryAssembly()?.GetName().Name ?? "Helix", "helix-settings.json");
    }

    /// <summary>The JSON file.</summary>
    public string FilePath => _path;

    private Dictionary<string, string> Values
    {
        get
        {
            if (_values is not null) return _values;
            try
            {
                _values = File.Exists(_path) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_path)) ?? [] : [];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _values = []; // unreadable file: start from the defaults
            }

            return _values;
        }
    }

    public string? Get(string key) => Values.GetValueOrDefault(key);

    public void Set(string key, string? value)
    {
        if (value is null) Values.Remove(key);
        else Values[key] = value;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Values));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceWarning($"Helix settings not saved: {ex.Message}");
        }
    }
}

/// <summary>
/// Remembers what the user arranged: put helix:HelixSettings.RememberAs="key" on a HelixDashboard (layout),
/// a HelixDataGrid (column order, width and visibility, page size) or a HelixFilterBar (saved filters).
/// </summary>
public static class HelixSettings
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The store used by every control (a JSON file per user by default).</summary>
    public static IHelixSettingsStore Store { get; set; } = new HelixFileSettingsStore();

    public static T? Load<T>(string key)
    {
        var json = Store.Get(key);
        if (string.IsNullOrEmpty(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return default; }
    }

    public static void Save<T>(string key, T? value) => Store.Set(key, value is null ? null : JsonSerializer.Serialize(value, Json));

    public static readonly AttachedProperty<string?> RememberAsProperty = AvaloniaProperty.RegisterAttached<Control, string?>("RememberAs", typeof(HelixSettings));

    public static string? GetRememberAs(Control element) => element.GetValue(RememberAsProperty);
    public static void SetRememberAs(Control element, string? value) => element.SetValue(RememberAsProperty, value);

    static HelixSettings() => RememberAsProperty.Changed.AddClassHandler<Control>((control, e) =>
    {
        if (e.OldValue is not null || e.NewValue is not string { Length: > 0 }) return;
        switch (control)
        {
            case HelixDashboard dashboard: Attach(dashboard); break;
            case HelixDataGrid grid: Attach(grid); break;
            case HelixFilterBar bar: Attach(bar); break;
        }
    });

    // ----- Dashboard: the layout -----------------------------------------------------------------

    private static void Attach(HelixDashboard dashboard)
    {
        dashboard.AttachedToVisualTree += (_, _) =>
        {
            if (dashboard.Layout is null && Load<List<HelixTileState>>("dashboard:" + GetRememberAs(dashboard)) is { Count: > 0 } saved) dashboard.Layout = saved;
        };
        dashboard.LayoutChanged += (_, _) => Save("dashboard:" + GetRememberAs(dashboard), dashboard.Layout);
    }

    // ----- Data grid: columns and page size --------------------------------------------------------

    private sealed record GridColumn(string Key, int Order, double? Width, bool Visible);

    private sealed record GridState(List<GridColumn> Columns, int PageSize);

    private static string KeyOf(DataGridColumn column) => column.Header?.ToString() is { Length: > 0 } header ? header : column.SortMemberPath ?? "";

    /// <summary>Saves a grid's columns and page size now (done when it leaves the screen).</summary>
    public static void SaveGrid(HelixDataGrid grid)
    {
        if (GetRememberAs(grid) is not { Length: > 0 } key) return;
        Save("grid:" + key, new GridState(
            grid.Columns.Select(c => new GridColumn(KeyOf(c), c.DisplayIndex, c.Width.IsAbsolute ? c.ActualWidth : null, c.IsVisible)).ToList(),
            grid.PageSize));
    }

    private static void Attach(HelixDataGrid grid)
    {
        var restored = false;
        grid.AttachedToVisualTree += (_, _) =>
        {
            if (restored || Load<GridState>("grid:" + GetRememberAs(grid)) is not { } state) return;
            restored = true;
            foreach (var saved in state.Columns.OrderBy(c => c.Order))
            {
                if (grid.Columns.FirstOrDefault(c => KeyOf(c) == saved.Key) is not { } column) continue;
                if (saved.Order >= 0 && saved.Order < grid.Columns.Count) column.DisplayIndex = saved.Order;
                if (saved.Width is { } width and > 0) column.Width = new DataGridLength(width);
                column.IsVisible = saved.Visible;
            }

            if (state.PageSize > 0) grid.PageSize = state.PageSize;
        };
        grid.DetachedFromVisualTree += (_, _) => SaveGrid(grid);
    }

    // ----- Filter bar: the saved filters ------------------------------------------------------------

    private sealed record StoredPreset(string Name, Dictionary<string, string> State);

    private static void Attach(HelixFilterBar bar)
    {
        bar.AttachedToVisualTree += (_, _) =>
        {
            if (bar.Presets is not null) return;
            var presets = new ObservableCollection<HelixFilterPreset>(
                (Load<List<StoredPreset>>("filters:" + GetRememberAs(bar)) ?? []).Select(p => new HelixFilterPreset(p.Name, p.State)));
            presets.CollectionChanged += (_, _) => Save("filters:" + GetRememberAs(bar),
                presets.Select(p => new StoredPreset(p.Name, p.State.ToDictionary(kv => kv.Key, kv => kv.Value))).ToList());
            bar.Presets = presets;
        };
    }
}
