using System.Globalization;

namespace Helix.Blazor;

/// <summary>
/// Every text the components write on their own: buttons, empty states, screen-reader labels and announcements.
/// English and French are built in and picked from the UI culture; change any of them in one place:
/// <code>
/// builder.Services.AddHelix(o => o.Texts = HelixTexts.English with { Cancel = "Back", NoData = "Nothing yet" });
/// </code>
/// Texts with <c>{0}</c>, <c>{1}</c>… are format strings.
/// </summary>
public sealed record HelixTexts
{
    // ----- Common -----------------------------------------------------------------------
    public string Close { get; init; } = "Close";
    public string Clear { get; init; } = "Clear";
    public string Loading { get; init; } = "Loading";
    public string NoData { get; init; } = "No data";
    public string Confirm { get; init; } = "Confirm";
    public string Cancel { get; init; } = "Cancel";
    public string Delete { get; init; } = "Delete?";
    public string Previous { get; init; } = "Previous";
    public string Next { get; init; } = "Next";
    public string Expand { get; init; } = "Expand";
    public string Collapse { get; init; } = "Collapse";
    public string Retry { get; init; } = "Retry";
    public string Reset { get; init; } = "Reset";
    /// <summary>{0} = position, {1} = count.</summary>
    public string PositionOf { get; init; } = "{0} of {1}";
    public string Required { get; init; } = "required";

    // ----- Fields -------------------------------------------------------------------------
    public string SelectPlaceholder { get; init; } = "Select…";
    public string Decrease { get; init; } = "Decrease";
    public string Increase { get; init; } = "Increase";
    public string ClearDate { get; init; } = "Clear the date";
    public string ChooseDate { get; init; } = "Choose a date";
    public string Today { get; init; } = "Today";
    public string Now { get; init; } = "Now";
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
    public string PreviousMonth { get; init; } = "Previous month";
    public string NextMonth { get; init; } = "Next month";
    public string DateKeyboardHelp { get; init; } = "Arrows · Enter · Page ↑↓";
    public string ChooseColor { get; init; } = "Choose a color";
    public string SaturationBrightness { get; init; } = "Saturation and brightness";
    /// <summary>{0} = saturation %, {1} = brightness %.</summary>
    public string SaturationBrightnessValue { get; init; } = "Saturation {0} %, brightness {1} %";
    public string Opacity { get; init; } = "Opacity";
    public string HexCode { get; init; } = "Hex code";
    public string PickFromScreen { get; init; } = "Pick a color from the screen";
    public string Eyedropper { get; init; } = "Eyedropper";
    public string SuggestedColors { get; init; } = "Suggested colors";
    public string Rating { get; init; } = "Rating";
    public string DropFilesHere { get; init; } = "Drop your files here";
    public string ReleaseToAdd { get; init; } = "Release to add";
    public string PerFile { get; init; } = " per file";
    public string OrClickToBrowse { get; init; } = "or click to browse";
    /// <summary>{0} = the file name.</summary>
    public string RemoveFile { get; init; } = "Remove {0}";

    // ----- Data grid and pagination ------------------------------------------------------------
    public string Search { get; init; } = "Search…";
    /// <summary>{0} = the search text.</summary>
    public string NoResultsFor { get; init; } = "No results for “{0}”";
    public string LoadError { get; init; } = "The data could not be loaded.";
    public string Columns { get; init; } = "Columns";
    public string VisibleColumns { get; init; } = "Visible columns";
    public string SelectAll { get; init; } = "Select all";
    public string SelectPage { get; init; } = "Select the page";
    public string SelectRow { get; init; } = "Select the row";
    /// <summary>{0} = number of selected rows.</summary>
    public string SelectedCount { get; init; } = "{0} selected";
    public string PreviousPage { get; init; } = "Previous page";
    public string NextPage { get; init; } = "Next page";
    /// <summary>{0} = page number.</summary>
    public string PageNumber { get; init; } = "Page {0}";
    /// <summary>{0} = first item, {1} = last item, {2} = total.</summary>
    public string RangeOf { get; init; } = "{0}–{1} of {2}";
    /// <summary>{0} = rows per page.</summary>
    public string RowsPerPage { get; init; } = "{0} / page";
    public string RowsPerPageLabel { get; init; } = "Rows per page";
    public string Rows { get; init; } = "Rows";
    public string Export { get; init; } = "Export";
    public string ShowDetails { get; init; } = "Show details";
    public string HideDetails { get; init; } = "Hide details";

    // ----- Charts ------------------------------------------------------------------------------
    public string Series { get; init; } = "Series";
    /// <summary>{0} = series name.</summary>
    public string ShowSeries { get; init; } = "Show {0}";
    /// <summary>{0} = series name.</summary>
    public string HideSeries { get; init; } = "Hide {0}";
    public string WindowStart { get; init; } = "Start of the visible period";
    public string WindowEnd { get; init; } = "End of the visible period";
    public string Category { get; init; } = "Category";
    public string Value { get; init; } = "Value";
    public string Share { get; init; } = "Share";
    public string Others { get; init; } = "Others";
    public string Total { get; init; } = "Total";
    public string Breakdown { get; init; } = "Breakdown";
    public string ShowLess { get; init; } = "Show less";
    /// <summary>{0} = number of rows.</summary>
    public string ShowAll { get; init; } = "Show all ({0})";

    // ----- Navigation and layout ---------------------------------------------------------------
    public string ShowFullPath { get; init; } = "Show the full path";
    public string Steps { get; init; } = "Steps";
    public string StepDone { get; init; } = "(done)";
    public string StepCurrent { get; init; } = "(current)";
    public string StepError { get; init; } = "(error)";
    public string StepErrorHint { get; init; } = "Needs fixing";
    public string ResizeHint { get; init; } = "Drag to resize · double-click to reset";
    public string ResizePanels { get; init; } = "Resize the panels";
    /// <summary>{0} = number of children.</summary>
    public string ExpandCount { get; init; } = "Expand ({0})";
    public string NothingPlanned { get; init; } = "Nothing planned in this period";
    public string TimelineEditHint { get; init; } = "Drag to move, arrows to shift, Shift + arrows to extend";

    // ----- Carousel --------------------------------------------------------------------------
    public string Carousel { get; init; } = "Carousel";
    public string CarouselRole { get; init; } = "carousel";
    public string SlideRole { get; init; } = "slide";
    public string PreviousSlide { get; init; } = "Previous slide";
    public string NextSlide { get; init; } = "Next slide";
    public string ChooseSlide { get; init; } = "Choose a slide";
    /// <summary>{0} = slide, {1} = count.</summary>
    public string SlideOf { get; init; } = "Slide {0} of {1}";
    public string Play { get; init; } = "Play";
    public string Pause { get; init; } = "Pause";

    // ----- Drag and drop -----------------------------------------------------------------------
    public string DraggableItem { get; init; } = "draggable item";
    public string DraggableCard { get; init; } = "draggable card";
    public string DropCardHere { get; init; } = "Drop a card here";
    public string SortableListHelp { get; init; } = "Reorderable list. Enter to grab, arrows to move, Enter to drop.";
    /// <summary>{0} = item position.</summary>
    public string ItemGrabbed { get; init; } = "Item {0} grabbed. Arrows to move, Enter to drop, Escape to cancel.";
    public string CardGrabbed { get; init; } = "Card grabbed. Arrows to move, Enter to drop, Escape to cancel.";
    /// <summary>{0} = position.</summary>
    public string DroppedAt { get; init; } = "Dropped at position {0}.";
    /// <summary>{0} = column, {1} = position.</summary>
    public string CardDroppedIn { get; init; } = "Card dropped in {0}, position {1}.";
    public string MoveCancelled { get; init; } = "Move cancelled.";
    /// <summary>{0} = position, {1} = count.</summary>
    public string PositionAnnouncement { get; init; } = "Position {0} of {1}.";
    /// <summary>{0} = tile title.</summary>
    public string TileLayout { get; init; } = "Layout of {0}";
    public string MoveBefore { get; init; } = "Move before";
    public string MoveAfter { get; init; } = "Move after";
    public string Narrower { get; init; } = "Narrower";
    public string Wider { get; init; } = "Wider";
    public string Width { get; init; } = "Width";
    /// <summary>{0} = tile title.</summary>
    public string HideTile { get; init; } = "Hide {0}";
    public string Customize { get; init; } = "Customize";
    public string Done { get; init; } = "Done";
    public string DashboardEditHint { get; init; } = "Drag the tiles to rearrange them, change their width or hide them.";

    /// <summary>Formats a text that has {0}, {1}… placeholders.</summary>
    public static string Format(string text, params object?[] args) => string.Format(CultureInfo.CurrentCulture, text, args);

    public static HelixTexts English { get; } = new();

    public static HelixTexts French { get; } = new()
    {
        Close = "Fermer", Clear = "Effacer", Loading = "Chargement", NoData = "Aucune donnée",
        Confirm = "Confirmer", Cancel = "Annuler", Delete = "Supprimer ?", Previous = "Précédent", Next = "Suivant",
        Expand = "Déplier", Collapse = "Replier", Retry = "Réessayer", Reset = "Réinitialiser",
        PositionOf = "{0} sur {1}", Required = "obligatoire",

        SelectPlaceholder = "Sélectionner…", Decrease = "Diminuer", Increase = "Augmenter",
        ClearDate = "Effacer la date", ChooseDate = "Choisir une date", Today = "Aujourd'hui", Now = "Maintenant", ChooseDateTime = "Choisir une date et une heure", ChooseTime = "Heure",
        ChoosePeriod = "Choisir une période", Yesterday = "Hier", Last7Days = "7 derniers jours", Last30Days = "30 derniers jours",
        ThisWeek = "Cette semaine", ThisMonth = "Ce mois-ci", LastMonth = "Le mois dernier", PickEndDate = "Choisissez maintenant le dernier jour", PreviousMonth = "Mois précédent", NextMonth = "Mois suivant",
        DateKeyboardHelp = "Flèches · Entrée · Page ↑↓",
        ChooseColor = "Choisir une couleur", SaturationBrightness = "Saturation et luminosité",
        SaturationBrightnessValue = "Saturation {0} %, luminosité {1} %", Opacity = "Opacité", HexCode = "Code hexadécimal",
        PickFromScreen = "Prendre une couleur à l'écran", Eyedropper = "Pipette", SuggestedColors = "Couleurs proposées",
        Rating = "Note", DropFilesHere = "Glissez vos fichiers ici", ReleaseToAdd = "Relâchez pour ajouter", PerFile = " par fichier",
        OrClickToBrowse = "ou cliquez pour parcourir", RemoveFile = "Retirer {0}",

        Search = "Rechercher…", NoResultsFor = "Aucun résultat pour « {0} »", LoadError = "Impossible de charger les données.",
        Columns = "Colonnes", VisibleColumns = "Colonnes affichées", SelectAll = "Tout sélectionner", SelectPage = "Sélectionner la page",
        SelectRow = "Sélectionner la ligne", SelectedCount = "{0} sélectionné(s)", PreviousPage = "Page précédente", NextPage = "Page suivante",
        PageNumber = "Page {0}", RangeOf = "{0}–{1} sur {2}", RowsPerPage = "{0} / page", RowsPerPageLabel = "Lignes par page",
        Rows = "Lignes", Export = "Exporter", ShowDetails = "Afficher le détail", HideDetails = "Masquer le détail",

        Series = "Séries", ShowSeries = "Afficher {0}", HideSeries = "Masquer {0}",
        WindowStart = "Début de la période affichée", WindowEnd = "Fin de la période affichée",
        Category = "Catégorie", Value = "Valeur", Share = "Part", Others = "Autres", Total = "Total", Breakdown = "Répartition",
        ShowLess = "Réduire", ShowAll = "Voir tout ({0})",

        ShowFullPath = "Afficher tout le chemin", Steps = "Étapes", StepDone = "(terminée)", StepCurrent = "(en cours)",
        StepError = "(en erreur)", StepErrorHint = "Erreur à corriger", ResizeHint = "Glisser pour redimensionner · double-clic pour réinitialiser",
        ResizePanels = "Redimensionner les panneaux",
        ExpandCount = "Déplier ({0})", NothingPlanned = "Rien de planifié sur cette période",
        TimelineEditHint = "Glisser pour déplacer, flèches pour décaler, Maj + flèches pour allonger",

        Carousel = "Carrousel", CarouselRole = "carrousel", SlideRole = "diapositive", PreviousSlide = "Diapositive précédente",
        NextSlide = "Diapositive suivante", ChooseSlide = "Choisir une diapositive", SlideOf = "Diapositive {0} sur {1}",
        Play = "Lancer le défilement", Pause = "Mettre en pause",

        DraggableItem = "élément déplaçable", DraggableCard = "carte déplaçable", DropCardHere = "Déposez une carte ici",
        SortableListHelp = "Liste réordonnable. Entrée pour saisir, flèches pour déplacer, Entrée pour déposer.",
        ItemGrabbed = "Élément {0} saisi. Flèches pour déplacer, Entrée pour déposer, Échap pour annuler.",
        CardGrabbed = "Carte saisie. Flèches pour déplacer, Entrée pour déposer, Échap pour annuler.",
        DroppedAt = "Déposé en position {0}.", CardDroppedIn = "Carte déposée dans {0}, position {1}.",
        MoveCancelled = "Déplacement annulé.", PositionAnnouncement = "Position {0} sur {1}.",
        TileLayout = "Disposition de {0}", MoveBefore = "Déplacer avant", MoveAfter = "Déplacer après",
        Narrower = "Plus étroit", Wider = "Plus large", Width = "Largeur", HideTile = "Masquer {0}",
        Customize = "Personnaliser", Done = "Terminé", DashboardEditHint = "Glissez les tuiles pour les réorganiser, changez leur largeur ou masquez-les.",
    };

    /// <summary>French for a French UI culture, English otherwise.</summary>
    public static HelixTexts For(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "fr" ? French : English;
}
