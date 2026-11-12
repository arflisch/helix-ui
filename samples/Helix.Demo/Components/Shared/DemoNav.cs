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
            new("Customization", "customization"),
            new("Light and dark themes", "themes"),
            new("Remembering user settings", "user-settings"),
            new("Developer helpers", "helpers"),
            new("Export: Excel, Word, CSV", "export"),
        ]),
        new("Actions",
        [
            new("Button", "components/button", "Four variants, every color, loading state and icons.", "sparkles"),
            new("ButtonGroup", "components/buttongroup", "Joined buttons, split button, toolbar.", "columns"),
            new("Menu", "components/menu", "Action menu: icons, shortcuts, checks, danger.", "chevron-down"),
        ]),
        new("Forms",
        [
            new("TextField", "components/textfield", "Text field with label, hint, error, icon and clear button.", "mail"),
            new("Select", "components/select", "Typed dropdown list, usable from the keyboard.", "chevron-down"),
            new("MultiSelect", "components/multiselect", "Several choices: chips, checkboxes, search.", "check"),
            new("Checkbox", "components/checkbox", "Animated checkbox, any check color.", "check"),
            new("Switch", "components/switch", "Toggle switch: track and thumb customizable with one parameter.", "settings"),
            new("Slider", "components/slider", "Slider with fill, format and colors.", "minus"),
            new("RadioGroup", "components/radiogroup", "One choice among several: classic, cards or segmented.", "check"),
            new("NumberField", "components/numberfield", "Number with − / +, bounds, step and unit.", "plus"),
            new("DatePicker", "components/datepicker", "Keyboard calendar, disabled dates, culture.", "calendar"),
            new("DateTimePicker", "components/datetimepicker", "Date and time, by quarter hour or any step.", "calendar"),
            new("DateRangePicker", "components/daterangepicker", "A period of days, with shortcuts.", "calendar"),
            new("ColorPicker", "components/colorpicker", "Color: area, hue, opacity, eyedropper.", "sparkles"),
            new("Rating", "components/rating", "Rating with stars or hearts.", "star"),
            new("AutoForm", "components/autoform", "A whole form from a class and its attributes.", "check"),
        ]),
        new("Data",
        [
            new("DataGrid", "components/datagrid", "Table: sorting, search, paging, selection, templates.", "calendar"),
            new("FilterBar", "components/filterbar", "Filters above a list, with saved filters.", "search"),
            new("Chart", "components/chart", "Lines, areas, steps, bars, thresholds, real time and zoom.", "arrow-up"),
            new("Timeline", "components/timeline", "Schedule with statuses, lanes and a live current-time line.", "calendar"),
            new("Stat", "components/stat", "Key indicator with change and sparkline.", "trending-up"),
            new("Heatmap", "components/heatmap", "Colored grid: single or diverging scale.", "columns"),
            new("BarList", "components/barlist", "Ranking as horizontal bars (top N).", "trending-up"),
            new("Gauge", "components/gauge", "Gauge with thresholds and a status label.", "zap"),
            new("TargetBar", "components/targetbar", "A value against its target, with tolerance.", "trending-up"),
            new("TimeBar", "components/timebar", "Countdown to the next deadline, with the slots around now.", "calendar"),
            new("Donut", "components/donut", "Parts of a whole, or a segmented bar.", "chevron-right"),
            new("TreeView", "components/treeview", "Hierarchy as a list or an org chart.", "columns"),
            new("LogViewer", "components/logviewer", "Live log: levels, search, follow.", "columns"),
        ]),
        new("Interaction",
        [
            new("Dashboard", "components/dashboard", "Tiles the user reorders, resizes and hides.", "layout"),
            new("Kanban", "components/kanban", "Cards dragged between columns, with limits and rules.", "columns"),
            new("SortableList", "components/sortable", "List reordered by dragging or from the keyboard.", "grip"),
            new("Splitter", "components/splitter", "Two resizable panes.", "columns"),
            new("FileDrop", "components/filedrop", "File drop with type and size checks.", "upload"),
        ]),
        new("Display",
        [
            new("Card", "components/card", "Container with title, actions and footer.", "copy"),
            new("Badge", "components/badge", "Soft, solid or outline status labels.", "star"),
            new("Avatar", "components/avatar", "Initials, image, status and automatic color.", "user"),
            new("Tooltip", "components/tooltip", "Lightweight tooltip in pure CSS.", "info"),
            new("Progress", "components/progress", "Progress bar and spinner.", "download"),
            new("Accordion", "components/accordion", "Animated collapsible sections.", "chevron-down"),
            new("EmptyState", "components/emptystate", "Empty screen that explains and offers an action.", "search"),
            new("DescriptionList", "components/descriptionlist", "Label / value pairs for detail screens.", "columns"),
            new("Skeleton", "components/skeleton", "Loading placeholders.", "minus"),
            new("Busy", "components/busy", "A loading veil over a zone.", "download"),
            new("Carousel", "components/carousel", "Slides by swipe, arrows or autoplay.", "arrow-right"),
        ]),
        new("Feedback",
        [
            new("Alert", "components/alert", "Contextual messages with severity.", "warning"),
            new("Toast", "components/toast", "Notifications from any service.", "bell"),
            new("Dialog", "components/dialog", "Accessible modal window.", "external"),
            new("Drawer", "components/drawer", "Side panel: right, left or bottom.", "external"),
            new("Confirm", "components/confirm", "if (await Confirm.AskAsync(…)) in one line.", "warning"),
        ]),
        new("Navigation",
        [
            new("Tabs", "components/tabs", "Underline, pills or segmented tabs.", "calendar"),
            new("Breadcrumb", "components/breadcrumb", "Collapsible breadcrumb.", "chevron-right"),
            new("Stepper", "components/stepper", "Steps of a flow, with errors.", "check"),
            new("Pagination", "components/pagination", "Self-contained paging: full or compact.", "chevron-right"),
        ]),
    ];

    public static IEnumerable<NavItem> Components => Groups.Skip(1).SelectMany(g => g.Items);
}
