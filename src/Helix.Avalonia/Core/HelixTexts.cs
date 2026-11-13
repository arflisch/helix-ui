using System.Globalization;

namespace Helix.Avalonia;

/// <summary>
/// The texts the controls write on their own (tooltips, placeholders, dialog buttons).
/// English and French are built in and picked from the UI culture; change them in one place:
/// <code>HelixTexts.Current = HelixTexts.English with { Cancel = "Back" };</code>
/// </summary>
public sealed record HelixTexts
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
    public string Previous { get; init; } = "Previous";
    public string Next { get; init; } = "Next";
    public string Search { get; init; } = "Search…";
    /// <summary>{0} = the search text.</summary>
    public string NoResultsFor { get; init; } = "No results for “{0}”";
    public string LoadError { get; init; } = "The data could not be loaded.";
    public string Retry { get; init; } = "Retry";
    public string Columns { get; init; } = "Columns";
    public string VisibleColumns { get; init; } = "Visible columns";
    public string Reset { get; init; } = "Reset";
    /// <summary>{0} = first row, {1} = last row, {2} = total.</summary>
    public string RangeOf { get; init; } = "{0}–{1} of {2}";
    /// <summary>{0} = number of rows.</summary>
    public string ShowAll { get; init; } = "Show all ({0})";
    public string ShowLess { get; init; } = "Show less";
    public string StepError { get; init; } = "Needs fixing";
    public string Others { get; init; } = "Others";
    public string Total { get; init; } = "Total";
    public string LogDebug { get; init; } = "Debug";
    public string LogInfo { get; init; } = "Info";
    public string LogWarning { get; init; } = "Warning";
    public string LogError { get; init; } = "Error";
    public string FollowLog { get; init; } = "Follow";
    public string ExpressionHint { get; init; } = "You can type a calculation, e.g. 120*0.8";
    public string Notifications { get; init; } = "Notifications";
    public string MarkAllRead { get; init; } = "Mark all as read";
    public string NoNotifications { get; init; } = "You're all caught up";
    /// <summary>{0} = number of unread notifications.</summary>
    public string UnreadCount { get; init; } = "{0} unread";
    public string Earlier { get; init; } = "Earlier";
    public string JustNow { get; init; } = "Just now";
    /// <summary>{0} = minutes.</summary>
    public string MinutesAgo { get; init; } = "{0} min ago";
    /// <summary>{0} = hours.</summary>
    public string HoursAgo { get; init; } = "{0} h ago";
    public string ClearAll { get; init; } = "Clear all";
    public string Dismiss { get; init; } = "Dismiss";
    /// <summary>{0} = the deadline time, e.g. "until 14:10".</summary>
    public string TimeBarUntil { get; init; } = "until {0}";
    public string TimeBarCurrent { get; init; } = "Current";
    public string TimeBarPassed { get; init; } = "Deadline passed";
    public string Save { get; init; } = "Save";
    public string Name { get; init; } = "Name";
    public string ClearFilters { get; init; } = "Clear filters";
    public string SavedFilters { get; init; } = "Saved filters";
    public string SaveFilters { get; init; } = "Save these filters…";
    public string NoSavedFilters { get; init; } = "No saved filters yet";
    public string DeleteSavedFilter { get; init; } = "Delete “{0}”";
    public string SelectAll { get; init; } = "Select all";
    public string Today { get; init; } = "Today";
    public string ChooseDateTime { get; init; } = "Choose a date and time";
    public string ChooseTime { get; init; } = "Time";
    public string ChoosePeriod { get; init; } = "Choose a period";
    public string Yesterday { get; init; } = "Yesterday";
    public string Last7Days { get; init; } = "Last 7 days";
    public string Last30Days { get; init; } = "Last 30 days";
    public string ThisWeek { get; init; } = "This week";
    public string ThisMonth { get; init; } = "This month";
    public string LastMonth { get; init; } = "Last month";
    public string PickEndDate { get; init; } = "Now pick the last day";
    public string Export { get; init; } = "Export";
    public string ShowDetails { get; init; } = "Show details";
    public string HideDetails { get; init; } = "Hide details";
    public string EditLayout { get; init; } = "Edit layout";
    public string Done { get; init; } = "Done";
    public string HiddenTiles { get; init; } = "Hidden tiles";
    public string MoveBefore { get; init; } = "Move before";
    public string MoveAfter { get; init; } = "Move after";
    public string MoveLeft { get; init; } = "Move to the left column";
    public string MoveRight { get; init; } = "Move to the right column";
    public string DragToMove { get; init; } = "Drag to move";
    public string Narrower { get; init; } = "Narrower";
    public string Wider { get; init; } = "Wider";
    public string Hide { get; init; } = "Hide";
    public string DropFilesHere { get; init; } = "Drop your files here";
    public string ReleaseToAdd { get; init; } = "Release to add";
    public string Browse { get; init; } = "Browse";
    /// <summary>{0} = file name, {1} = size limit.</summary>
    public string FileTooLarge { get; init; } = "{0} is larger than {1}.";
    public string Add { get; init; } = "Add";
    public string Now { get; init; } = "Now";
    public string Expand { get; init; } = "Expand";
    public string Collapse { get; init; } = "Collapse";
    public string NothingPlanned { get; init; } = "Nothing planned in this period";
    public string DropCardHere { get; init; } = "Drop a card here";
    public string Hex { get; init; } = "Hex";
    public string Opacity { get; init; } = "Opacity";

    public static HelixTexts English { get; } = new();

    public static HelixTexts French { get; } = new()
    {
        Today = "Aujourd'hui", ChooseDateTime = "Choisir une date et une heure", ChooseTime = "Heure", ChoosePeriod = "Choisir une période", Yesterday = "Hier", Last7Days = "7 derniers jours", Last30Days = "30 derniers jours", ThisWeek = "Cette semaine", ThisMonth = "Ce mois-ci", LastMonth = "Le mois dernier", PickEndDate = "Choisissez maintenant le dernier jour",
        SelectAll = "Tout sélectionner",
        Save = "Enregistrer", Name = "Nom", ClearFilters = "Effacer les filtres", SavedFilters = "Filtres enregistrés", SaveFilters = "Enregistrer ces filtres…", NoSavedFilters = "Aucun filtre enregistré", DeleteSavedFilter = "Supprimer « {0} »",
        LogDebug = "Debug", LogInfo = "Info", LogWarning = "Alerte", LogError = "Erreur", FollowLog = "Suivre", ExpressionHint = "Vous pouvez taper un calcul, par ex. 120*0,8", Notifications = "Notifications", MarkAllRead = "Tout marquer comme lu", NoNotifications = "Vous êtes à jour", UnreadCount = "{0} non lues", Earlier = "Plus ancien", JustNow = "À l'instant", MinutesAgo = "il y a {0} min", HoursAgo = "il y a {0} h", ClearAll = "Tout effacer", Dismiss = "Retirer", TimeBarUntil = "jusqu'à {0}", TimeBarCurrent = "En cours", TimeBarPassed = "Échéance dépassée",
        Export = "Exporter", ShowDetails = "Afficher le détail", HideDetails = "Masquer le détail",
        Close = "Fermer", Clear = "Effacer", Confirm = "Confirmer", Cancel = "Annuler", Loading = "Chargement",
        NoData = "Aucune donnée", SelectPlaceholder = "Sélectionner…", ChooseDate = "Choisir une date",
        Increase = "Augmenter", Decrease = "Diminuer", Required = "obligatoire",
        Previous = "Précédent", Next = "Suivant", Search = "Rechercher…", NoResultsFor = "Aucun résultat pour « {0} »",
        LoadError = "Impossible de charger les données.", Retry = "Réessayer", Columns = "Colonnes", VisibleColumns = "Colonnes affichées",
        Reset = "Réinitialiser", RangeOf = "{0}–{1} sur {2}", ShowAll = "Voir tout ({0})", ShowLess = "Réduire", StepError = "Erreur à corriger",
        Others = "Autres", Total = "Total", EditLayout = "Modifier la disposition", Done = "Terminé", HiddenTiles = "Tuiles masquées",
        MoveBefore = "Déplacer avant", MoveAfter = "Déplacer après", MoveLeft = "Colonne de gauche", MoveRight = "Colonne de droite", DragToMove = "Glisser pour déplacer", Narrower = "Plus étroit", Wider = "Plus large", Hide = "Masquer",
        DropFilesHere = "Glissez vos fichiers ici", ReleaseToAdd = "Relâchez pour ajouter", Browse = "Parcourir",
        FileTooLarge = "{0} dépasse {1}.", Add = "Ajouter", Now = "Maintenant", Expand = "Déplier", Collapse = "Replier",
        NothingPlanned = "Rien de planifié sur cette période", DropCardHere = "Déposez une carte ici", Hex = "Hex", Opacity = "Opacité",
    };

    /// <summary>Formats a text with {0}, {1}… placeholders.</summary>
    public static string Format(string text, params object?[] args) => string.Format(CultureInfo.CurrentCulture, text, args);

    private static HelixTexts? _current;

    /// <summary>The texts in use. Null (default): French for a French UI culture, English otherwise.</summary>
    public static HelixTexts Current
    {
        get => _current ?? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? French : English);
        set => _current = value;
    }
}
