using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class FieldsPage : UserControl
{
    public static string[] RatingLabels { get; } = ["Mauvais", "Moyen", "Bien", "Très bien", "Excellent"];

    public FieldsPage()
    {
        InitializeComponent();
        RequestForm.Model = new ReserveRequest();
        Sites.ItemsSource = new[] { "Lyon", "Grenoble", "Marseille", "Bordeaux", "Lille", "Nantes", "Toulouse", "Strasbourg", "Rennes", "Nice" };
        Sites.SelectedItems = new System.Collections.ObjectModel.ObservableCollection<string> { "Lyon", "Lille" };
        Validate();
    }

    private void OnEmailChanged(object sender, TextChangedEventArgs e) => Validate();

    public enum Direction { Hausse, Baisse }

    public sealed class ReserveRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Le site est obligatoire."), System.ComponentModel.DataAnnotations.StringLength(40, MinimumLength = 3),
         System.ComponentModel.DataAnnotations.Display(Name = "Site", Order = 1)]
        public string Site { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Display(Name = "Sens", Order = 2)]
        public Direction Sens { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 250), HelixField(Unit = "MW", Step = 5),
         System.ComponentModel.DataAnnotations.Display(Name = "Puissance", Order = 3, Description = "Entre 1 et 250 MW")]
        public double Puissance { get; set; } = 50;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Choisissez un début."), System.ComponentModel.DataAnnotations.Display(Name = "Début", Order = 4)]
        public System.DateTime? Debut { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Confirmer automatiquement", Order = 5)]
        public bool Auto { get; set; } = true;

        [System.ComponentModel.DataAnnotations.StringLength(200), HelixField(Span = 2), System.ComponentModel.DataAnnotations.Display(Name = "Commentaire", Order = 6)]
        public string? Commentaire { get; set; }
    }

    private void OnFormSubmitted(object sender, System.Windows.RoutedEventArgs e) =>
        HelixToasts.Success($"Demande pour {((ReserveRequest)RequestForm.Model!).Site} envoyée.");

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
