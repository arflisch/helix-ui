using System.Globalization;
using System.Text;

namespace Velo.Blazor;

/// <summary>
/// App-wide settings: the built-in texts and the default look and behavior of each component.
/// Set them once, in Program.cs or on <c>VeloProvider Options</c>; a parameter set on a component always wins.
/// </summary>
/// <example>
/// builder.Services.AddVelo(o =>
/// {
///     o.Defaults.Switch.Color = "#ff8a00";
///     o.Defaults.Button.Radius = "6px";
///     o.Defaults.DataGrid.Density = VeloSize.Sm;
///     o.Texts = VeloTexts.English;   // null (default): English or French from the UI culture
/// });
/// </example>
public sealed class VeloOptions
{
    /// <summary>Used when no provider and no registration exist.</summary>
    internal static VeloOptions Fallback { get; } = new();

    /// <summary>The built-in texts. Null: French for a French UI culture, English otherwise.</summary>
    public VeloTexts? Texts { get; set; }

    public VeloDefaults Defaults { get; } = new();

    /// <summary>How long a toast stays visible by default.</summary>
    public TimeSpan ToastDuration { get; set; } = TimeSpan.FromSeconds(4);

    public VeloTexts TextsFor(CultureInfo culture) => Texts ?? VeloTexts.For(culture);
}

/// <summary>Default values per component. Every property is optional (null keeps the built-in default).</summary>
public sealed class VeloDefaults
{
    /// <summary>Size of buttons, switches and checkboxes when neither the component nor its section sets one.</summary>
    public VeloSize? Size { get; set; }

    public ButtonDefaults Button { get; } = new();
    public SwitchDefaults Switch { get; } = new();
    public CheckboxDefaults Checkbox { get; } = new();

    /// <summary>Text, number, select and date fields.</summary>
    public FieldDefaults Field { get; } = new();
    public CardDefaults Card { get; } = new();
    public BadgeDefaults Badge { get; } = new();
    public DataGridDefaults DataGrid { get; } = new();
    public DialogDefaults Dialog { get; } = new();
    public TooltipDefaults Tooltip { get; } = new();

    public sealed class ButtonDefaults
    {
        public VeloVariant? Variant { get; set; }
        public VeloSize? Size { get; set; }
        public string? Color { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class SwitchDefaults
    {
        public VeloSize? Size { get; set; }
        public VeloLabelPosition? LabelPosition { get; set; }
        /// <summary>Track color when ON.</summary>
        public string? Color { get; set; }
        /// <summary>Track color when OFF.</summary>
        public string? TrackColor { get; set; }
        public string? ThumbColor { get; set; }
        public string? ThumbOnColor { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class CheckboxDefaults
    {
        public string? Color { get; set; }
        public string? CheckColor { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class FieldDefaults
    {
        /// <summary>Focus color.</summary>
        public string? Color { get; set; }
        public string? Radius { get; set; }
        public string? Background { get; set; }
        public string? BorderColor { get; set; }
        /// <summary>Control height, e.g. "34px" for compact forms.</summary>
        public string? Height { get; set; }
        /// <summary>The − / + buttons of number fields.</summary>
        public bool? ShowSteppers { get; set; }
        /// <summary>The clear (×) button of text and date fields.</summary>
        public bool? Clearable { get; set; }
    }

    public sealed class CardDefaults
    {
        public bool? Elevated { get; set; }
        public string? Padding { get; set; }
        public string? Radius { get; set; }
        public string? Background { get; set; }
        public string? BorderColor { get; set; }
    }

    public sealed class BadgeDefaults
    {
        public VeloBadgeVariant? Variant { get; set; }
        public string? Color { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class DataGridDefaults
    {
        public VeloSize? Density { get; set; }
        public bool? Striped { get; set; }
        public int? PageSize { get; set; }
        public string? Color { get; set; }
        public string? HeaderBackground { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class DialogDefaults
    {
        public string? Width { get; set; }
        public string? Radius { get; set; }
    }

    public sealed class TooltipDefaults
    {
        public VeloPlacement? Placement { get; set; }
        public string? Background { get; set; }
        public string? TextColor { get; set; }
        public string? Delay { get; set; }
    }

    /// <summary>
    /// The visual defaults as CSS variables on the provider element: components read them like their own parameters,
    /// and a parameter set on a component (inline variable) overrides them.
    /// </summary>
    internal string Declarations()
    {
        var css = new StringBuilder();
        void Var(string name, string? value) => VeloPalette.Append(css, name, value);
        void Color(string component, string? color)
        {
            if (!string.IsNullOrWhiteSpace(color))
            {
                var (value, on) = VeloColor.Resolve(color);
                Var($"--velo-{component}-color", value);
                Var($"--velo-{component}-on-color", on);
            }
        }

        Color("btn", Button.Color);
        Var("--velo-btn-radius", Button.Radius);

        Color("switch", Switch.Color);
        Var("--velo-switch-track", Switch.TrackColor);
        Var("--velo-switch-thumb", Switch.ThumbColor);
        Var("--velo-switch-thumb-on", Switch.ThumbOnColor);
        Var("--velo-switch-radius", Switch.Radius);

        Color("check", Checkbox.Color);
        Var("--velo-check-mark", Checkbox.CheckColor);
        Var("--velo-check-radius", Checkbox.Radius);

        Color("input", Field.Color);
        Var("--velo-input-radius", Field.Radius);
        Var("--velo-input-bg", Field.Background);
        Var("--velo-input-border", Field.BorderColor);
        Var("--velo-input-height", Field.Height);

        Var("--velo-card-padding", Card.Padding);
        Var("--velo-card-radius", Card.Radius);
        Var("--velo-card-bg", Card.Background);
        Var("--velo-card-border", Card.BorderColor);

        Color("badge", Badge.Color);
        Var("--velo-badge-radius", Badge.Radius);

        Color("grid", DataGrid.Color);
        Var("--velo-grid-header-bg", DataGrid.HeaderBackground);
        Var("--velo-grid-radius", DataGrid.Radius);

        Var("--velo-dialog-width", Dialog.Width);
        Var("--velo-dialog-radius", Dialog.Radius);

        Var("--velo-tooltip-bg", Tooltip.Background);
        Var("--velo-tooltip-text", Tooltip.TextColor);
        Var("--velo-tooltip-delay", Tooltip.Delay);
        return css.ToString();
    }
}
