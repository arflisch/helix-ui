namespace Velo.Wpf;

/// <summary>Semantic color of a control. A custom <c>Accent</c> brush replaces it.</summary>
public enum VeloTone { Primary, Neutral, Success, Warning, Danger, Info }

public enum VeloVariant { Solid, Soft, Outline, Ghost }

public enum VeloBadgeVariant { Soft, Solid, Outline }

public enum VeloSize { Sm, Md, Lg }

public enum VeloSeverity { Info, Success, Warning, Danger }

/// <summary>Side of a control where its label goes.</summary>
public enum VeloLabelPosition { End, Start }

/// <summary>Light, dark, or following Windows (System).</summary>
public enum VeloThemeMode { Light, Dark, System }
