namespace Helix.Demo.Components.Shared;

public sealed record NavItem(string Title, string Href, string Description = "", string Icon = "");

public sealed record NavGroup(string Title, NavItem[] Items);

public static class DemoNav
{
    public static readonly NavGroup[] Groups =
    [
        new("Guide",
        [
            new("Introduction", ""),
            new("Installation", "getting-started"),
            new("Personnalisation", "customization"),
            new("Thèmes clair et sombre", "themes"),
        ]),
        new("Actions",
        [
            new("Button", "components/button", "Quatre variantes, toutes les couleurs, chargement et icônes.", "sparkles"),
            new("ButtonGroup", "components/buttongroup", "Boutons accolés, split button, barre d'outils.", "columns"),
            new("Menu", "components/menu", "Menu d'actions : icônes, raccourcis, cases, danger.", "chevron-down"),
        ]),
        new("Formulaires",
        [
            new("TextField", "components/textfield", "Champ texte avec label, aide, erreur, icône et effacement.", "mail"),
            new("Select", "components/select", "Liste déroulante typée, utilisable au clavier.", "chevron-down"),
            new("Checkbox", "components/checkbox", "Case à cocher animée, couleur de coche libre.", "check"),
            new("Switch", "components/switch", "Interrupteur : piste et pastille personnalisables en un paramètre.", "settings"),
            new("Slider", "components/slider", "Curseur avec remplissage, format et couleurs.", "minus"),
            new("RadioGroup", "components/radiogroup", "Un choix parmi plusieurs : classique, cartes ou segmenté.", "check"),
            new("NumberField", "components/numberfield", "Nombre avec − / +, bornes, pas et unité.", "plus"),
            new("DatePicker", "components/datepicker", "Calendrier au clavier, dates interdites, culture.", "calendar"),
            new("ColorPicker", "components/colorpicker", "Couleur : zone, teinte, opacité, pipette.", "sparkles"),
            new("Rating", "components/rating", "Note en étoiles ou en cœurs.", "star"),
        ]),
        new("Données",
        [
            new("DataGrid", "components/datagrid", "Tableau : tri, recherche, pagination, sélection, templates.", "calendar"),
            new("Chart", "components/chart", "Lignes, aires, paliers, barres, seuils, temps réel et zoom.", "arrow-up"),
            new("Timeline", "components/timeline", "Planning avec statuts, lignes et heure courante en direct.", "calendar"),
            new("Stat", "components/stat", "Indicateur clé avec variation et mini-courbe.", "trending-up"),
            new("Heatmap", "components/heatmap", "Grille colorée : échelle simple ou divergente.", "columns"),
            new("BarList", "components/barlist", "Classement en barres horizontales (top N).", "trending-up"),
            new("Gauge", "components/gauge", "Jauge avec seuils et libellé d'état.", "zap"),
            new("Donut", "components/donut", "Répartition d'un tout, ou barre segmentée.", "chevron-right"),
            new("TreeView", "components/treeview", "Hiérarchie en liste ou en organigramme.", "columns"),
        ]),
        new("Interaction",
        [
            new("Dashboard", "components/dashboard", "Tuiles que l'utilisateur réorganise, redimensionne et masque.", "layout"),
            new("Kanban", "components/kanban", "Cartes à glisser entre colonnes, limites et règles.", "columns"),
            new("SortableList", "components/sortable", "Liste à réordonner au glisser ou au clavier.", "grip"),
            new("Splitter", "components/splitter", "Deux panneaux redimensionnables.", "columns"),
            new("FileDrop", "components/filedrop", "Dépôt de fichiers avec contrôles de type et de taille.", "upload"),
        ]),
        new("Affichage",
        [
            new("Card", "components/card", "Conteneur avec titre, actions et pied.", "copy"),
            new("Badge", "components/badge", "Étiquettes de statut soft, solid ou outline.", "star"),
            new("Avatar", "components/avatar", "Initiales, image, statut et couleur automatique.", "user"),
            new("Tooltip", "components/tooltip", "Infobulle légère en pur CSS.", "info"),
            new("Progress", "components/progress", "Barre de progression et spinner.", "download"),
            new("Accordion", "components/accordion", "Sections repliables animées.", "chevron-down"),
            new("EmptyState", "components/emptystate", "Écran vide qui explique et propose une action.", "search"),
            new("Skeleton", "components/skeleton", "Silhouettes de chargement.", "minus"),
            new("Carousel", "components/carousel", "Diapositives au doigt, aux flèches ou automatiques.", "arrow-right"),
        ]),
        new("Feedback",
        [
            new("Alert", "components/alert", "Messages contextuels avec sévérité.", "warning"),
            new("Toast", "components/toast", "Notifications depuis n'importe quel service.", "bell"),
            new("Dialog", "components/dialog", "Fenêtre modale accessible.", "external"),
            new("Drawer", "components/drawer", "Panneau latéral : droite, gauche ou bas.", "external"),
            new("Confirm", "components/confirm", "if (await Confirm.AskAsync(…)) en une ligne.", "warning"),
        ]),
        new("Navigation",
        [
            new("Tabs", "components/tabs", "Onglets underline, pills ou segmented.", "calendar"),
            new("Breadcrumb", "components/breadcrumb", "Fil d'Ariane repliable.", "chevron-right"),
            new("Stepper", "components/stepper", "Étapes d'un parcours, avec erreurs.", "check"),
            new("Pagination", "components/pagination", "Pages autonomes : complète ou compacte.", "chevron-right"),
        ]),
    ];

    public static IEnumerable<NavItem> Components => Groups.Skip(1).SelectMany(g => g.Items);
}
