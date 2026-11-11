using System.Windows;
using System.Windows.Markup;

// Default control styles live in Themes/Generic.xaml.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// One namespace in XAML: xmlns:velo="https://velo-ui.dev/wpf"
[assembly: XmlnsDefinition("https://velo-ui.dev/wpf", "Velo.Wpf")]
[assembly: XmlnsPrefix("https://velo-ui.dev/wpf", "velo")]
