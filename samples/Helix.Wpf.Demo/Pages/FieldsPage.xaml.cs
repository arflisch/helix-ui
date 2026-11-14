using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class FieldsPage : UserControl
{
    public static string[] RatingLabels { get; } = ["Poor", "Average", "Good", "Very good", "Excellent"];

    public FieldsPage()
    {
        InitializeComponent();
        RequestForm.Model = new ReserveRequest();
        Sites.ItemsSource = new[] { "Lyon", "Grenoble", "Marseille", "Bordeaux", "Lille", "Nantes", "Toulouse", "Strasbourg", "Rennes", "Nice" };
        Sites.SelectedItems = new System.Collections.ObjectModel.ObservableCollection<string> { "Lyon", "Lille" };
        Validate();
    }

    private void OnEmailChanged(object sender, TextChangedEventArgs e) => Validate();

    public enum Direction { Up, Down }

    public sealed class ReserveRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "The site is required."), System.ComponentModel.DataAnnotations.StringLength(40, MinimumLength = 3),
         System.ComponentModel.DataAnnotations.Display(Name = "Site", Order = 1)]
        public string Site { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Display(Name = "Direction", Order = 2)]
        public Direction Way { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 250), HelixField(Unit = "MW", Step = 5),
         System.ComponentModel.DataAnnotations.Display(Name = "Power", Order = 3, Description = "Between 1 and 250 MW")]
        public double Power { get; set; } = 50;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Pick a start."), System.ComponentModel.DataAnnotations.Display(Name = "Start", Order = 4)]
        public System.DateTime? Start { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Confirm automatically", Order = 5)]
        public bool Auto { get; set; } = true;

        [System.ComponentModel.DataAnnotations.StringLength(200), HelixField(Span = 2), System.ComponentModel.DataAnnotations.Display(Name = "Comment", Order = 6)]
        public string? Comment { get; set; }
    }

    private void OnFormSubmitted(object sender, System.Windows.RoutedEventArgs e) =>
        HelixToasts.Success($"Request for {((ReserveRequest)RequestForm.Model!).Site} sent.");

    private void Validate()
    {
        if (Email is null)
        {
            return;
        }

        var text = Email.Text ?? "";
        Email.Error = text.Contains('@') && text.Contains('.') ? null : "Incomplete address.";
        Email.Hint = "We never send ads.";
    }
}
