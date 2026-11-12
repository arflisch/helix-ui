namespace Helix.Wpf;

/// <summary>Semantic color of a control. A custom <c>Accent</c> brush replaces it.</summary>
public enum HelixTone { Primary, Neutral, Success, Warning, Danger, Info }

public enum HelixVariant { Solid, Soft, Outline, Ghost }

public enum HelixBadgeVariant { Soft, Solid, Outline }

public enum HelixSize { Sm, Md, Lg }

public enum HelixSeverity { Info, Success, Warning, Danger }

/// <summary>Side of a control where its label goes.</summary>
public enum HelixLabelPosition { End, Start }

/// <summary>Light, dark, or following Windows (System).</summary>
public enum HelixThemeMode { Light, Dark, System }
