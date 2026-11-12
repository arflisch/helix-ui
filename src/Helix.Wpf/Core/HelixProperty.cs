using System.Windows;

namespace Helix.Wpf;

/// <summary>Short dependency property registration for the controls.</summary>
internal static class HelixProperty
{
    public static DependencyProperty Register<TOwner, TValue>(string name, TValue defaultValue,
        PropertyChangedCallback? changed = null, FrameworkPropertyMetadataOptions options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender) =>
        DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(defaultValue, options, changed));
}
