using System.Windows;

namespace Velo.Wpf;

/// <summary>Short dependency property registration for the controls.</summary>
internal static class VeloProperty
{
    public static DependencyProperty Register<TOwner, TValue>(string name, TValue defaultValue,
        PropertyChangedCallback? changed = null, FrameworkPropertyMetadataOptions options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender) =>
        DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(defaultValue, options, changed));
}
