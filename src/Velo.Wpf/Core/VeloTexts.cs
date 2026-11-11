using System.Globalization;

namespace Velo.Wpf;

/// <summary>
/// The texts the controls write on their own (tooltips, placeholders, dialog buttons).
/// English and French are built in and picked from the UI culture; change them in one place:
/// <code>VeloTexts.Current = VeloTexts.English with { Cancel = "Back" };</code>
/// </summary>
public sealed record VeloTexts
{
    public string Close { get; init; } = "Close";
    public string Clear { get; init; } = "Clear";
    public string Confirm { get; init; } = "Confirm";
    public string Cancel { get; init; } = "Cancel";
    public string Loading { get; init; } = "Loading";
    public string NoData { get; init; } = "No data";
    public string SelectPlaceholder { get; init; } = "Select…";
    public string ChooseDate { get; init; } = "Choose a date";
    public string Increase { get; init; } = "Increase";
    public string Decrease { get; init; } = "Decrease";
    public string Required { get; init; } = "required";

    public static VeloTexts English { get; } = new();

    public static VeloTexts French { get; } = new()
    {
        Close = "Fermer", Clear = "Effacer", Confirm = "Confirmer", Cancel = "Annuler", Loading = "Chargement",
        NoData = "Aucune donnée", SelectPlaceholder = "Sélectionner…", ChooseDate = "Choisir une date",
        Increase = "Augmenter", Decrease = "Diminuer", Required = "obligatoire",
    };

    private static VeloTexts? _current;

    /// <summary>The texts in use. Null (default): French for a French UI culture, English otherwise.</summary>
    public static VeloTexts Current
    {
        get => _current ?? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? French : English);
        set => _current = value;
    }
}
