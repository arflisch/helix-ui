using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public enum ContractKind { DayAhead, Intraday, Balancing }

/// <summary>The edited model: the form is built from these properties and attributes.</summary>
public sealed class SiteForm : INotifyPropertyChanged
{
    private string? _name = "North plant";
    private double _capacity = 480;

    [Display(Name = "Site name", Order = 1), Required, StringLength(40, MinimumLength = 3)]
    public string? Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }

    [Display(Order = 2), Range(0, 1000), HelixField(Unit = "MW", Step = 10)]
    public double Capacity { get => _capacity; set { _capacity = value; PropertyChanged?.Invoke(this, new(nameof(Capacity))); } }

    [Display(Name = "Contract", Order = 3)]
    public ContractKind Kind { get; set; } = ContractKind.DayAhead;

    [Display(Name = "Commissioned", Order = 4), HelixField(WithTime = false)]
    public DateTime Commissioned { get; set; } = new(2019, 3, 12);

    [Display(Name = "Contact email", Order = 5), EmailAddress, HelixField(Placeholder = "name@company.com")]
    public string? Email { get; set; }

    [Display(Name = "Active", Order = 6, Description = "Shown in the planning")]
    public bool IsActive { get; set; } = true;

    [Display(Order = 7), HelixField(Span = 2, Placeholder = "Maintenance windows, limits…")]
    public string? Notes { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class FormsMorePage : UserControl
{
    public FormsMorePage()
    {
        AvaloniaXamlLoader.Load(this);
        var form = this.FindControl<HelixAutoForm>("Form")!;
        var model = new SiteForm();
        form.Model = model;
        var result = this.FindControl<TextBlock>("FormResult")!;
        form.Submitted += (_, _) => result.Text = $"Saved: {model.Name}, {model.Capacity} MW, {model.Kind}";
        form.Cancelled += (_, _) => result.Text = "Cancelled.";
        this.FindControl<HelixTargetBar>("Banded")!.Bands = [new(355, 405, HelixTone.Success, "Tolerance"), new(330, 355, HelixTone.Warning), new(405, 430, HelixTone.Warning)];
    }
}
