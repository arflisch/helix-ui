using System.Windows;
using System.Windows.Markup;

// Default control styles live in Themes/Generic.xaml.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// One namespace in XAML: xmlns:helix="https://helix-ui.dev/wpf"
[assembly: XmlnsDefinition("https://helix-ui.dev/wpf", "Helix.Wpf")]
[assembly: XmlnsPrefix("https://helix-ui.dev/wpf", "helix")]
