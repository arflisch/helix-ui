using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class FieldsPage : UserControl
{
    public static string[] RatingLabels { get; } = ["Mauvais", "Moyen", "Bien", "Très bien", "Excellent"];

    public FieldsPage()
    {
        InitializeComponent();
        Sites.ItemsSource = new[] { "Lyon", "Grenoble", "Marseille", "Bordeaux", "Lille", "Nantes", "Toulouse", "Strasbourg", "Rennes", "Nice" };
        Sites.SelectedItems = new System.Collections.ObjectModel.ObservableCollection<string> { "Lyon", "Lille" };
        Validate();
    }

    private void OnEmailChanged(object sender, TextChangedEventArgs e) => Validate();

    private void Validate()
    {
        if (Email is null)
        {
            return;
        }

        var text = Email.Text ?? "";
        Email.Error = text.Contains('@') && text.Contains('.') ? null : "Adresse incomplète.";
        Email.Hint = "Nous n'envoyons jamais de publicité.";
    }
}
