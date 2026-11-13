using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class PickersPage : UserControl
{
    public PickersPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixSelect>("Region")!.ItemsSource = new[] { "North", "South", "East", "West", "Centre", "Overseas" };
        this.FindControl<HelixSelect>("Region")!.SelectedIndex = 0;
        var teams = this.FindControl<HelixMultiSelect>("Teams")!;
        teams.ItemsSource = new[] { "Engineering", "Design", "Product", "Sales", "Support", "Finance" };
        teams.SelectedItems = new AvaloniaList<object>(["Engineering", "Design", "Sales"]);

        var weekdays = new Func<DateTime, bool>(d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var delivery = this.FindControl<HelixDatePicker>("Delivery")!;
        delivery.IsDateDisabled = weekdays;
        delivery.Minimum = DateTime.Today;
        this.FindControl<HelixDateTimePicker>("Start")!.SelectedDateTime = DateTime.Today.AddHours(14).AddMinutes(15);
        this.FindControl<HelixDateRangePicker>("Period")!.SelectedRange = new HelixDateRange(DateTime.Today.AddDays(-6), DateTime.Today);
        var inline = this.FindControl<HelixCalendar>("Inline")!;
        inline.RangeStart = DateTime.Today.AddDays(-3);
        inline.RangeEnd = DateTime.Today.AddDays(4);
        inline.IsDateDisabled = weekdays;

        var delivery2 = this.FindControl<HelixRadioGroup>("Delivery2")!;
        delivery2.ItemsSource = new[] { "Standard (3–5 days)", "Express (24 h)", "Pick up in store" };
        delivery2.SelectedIndex = 0;
        var period = this.FindControl<HelixRadioGroup>("Period2")!;
        period.ItemsSource = new[] { "Day", "Week", "Month", "Year" };
        period.SelectedIndex = 1;
        var plans = this.FindControl<HelixRadioGroup>("Plans")!;
        plans.ItemsSource = new[]
        {
            new HelixRadioOption("Starter", "For one site, up to 5 users.", "user", "starter"),
            new HelixRadioOption("Team", "Several sites, shared dashboards.", "layout", "team"),
            new HelixRadioOption("Enterprise", "SSO, audit trail and support.", "lock", "enterprise"),
        };
        plans.SelectedIndex = 1;
    }
}
