namespace Helix.Blazor;

public enum HelixSize { Sm, Md, Lg }

public enum HelixVariant { Solid, Soft, Outline, Ghost }

public enum HelixBadgeVariant { Soft, Solid, Outline }

public enum HelixSeverity { Info, Success, Warning, Danger }

public enum HelixTabsVariant { Underline, Pills, Segmented }

public enum HelixAlign { Start, Center, End }

/// <summary>Total of a grid column: in the footer row and in each group header.</summary>
public enum HelixAggregate { None, Sum, Average, Min, Max, Count }

public enum HelixPlacement { Top, Bottom, Left, Right }

public enum HelixAvatarStatus { None, Online, Away, Busy, Offline }

public enum HelixToastPosition { TopRight, TopCenter, TopLeft, BottomRight, BottomCenter, BottomLeft }

/// <summary>System follows the operating system setting (and its changes, live).</summary>
public enum HelixThemeMode { Light, Dark, System }

/// <summary>Side of a control where its label goes.</summary>
public enum HelixLabelPosition { End, Start }
