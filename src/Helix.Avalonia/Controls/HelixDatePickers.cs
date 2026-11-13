using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>A period of whole days, both ends included (times are ignored).</summary>
public sealed record HelixDateRange
{
    public HelixDateRange(DateTime start, DateTime end)
    {
        (Start, End) = end.Date < start.Date ? (end.Date, start.Date) : (start.Date, end.Date);
    }

    public DateTime Start { get; }
    public DateTime End { get; }

    /// <summary>Number of days, both ends included.</summary>
    public int Days => (End - Start).Days + 1;

    public bool Contains(DateTime date) => date.Date >= Start && date.Date <= End;

    /// <summary>The day after End at 00:00: handy for "from &lt;= t &lt; to" queries.</summary>
    public DateTime EndExclusive => End.AddDays(1);
}

/// <summary>A shortcut of <see cref="HelixDateRangePicker"/>, e.g. "Last 7 days". Range receives today.</summary>
public sealed record HelixDateRangePreset(string Label, Func<DateTime, HelixDateRange> Range);

/// <summary>One time of the <see cref="HelixDateTimePicker"/> list.</summary>
public sealed record HelixTimeSlot(TimeSpan Time, string Text, bool IsEnabled);

/// <summary>
/// A month calendar: keyboard and mouse, minimum / maximum, disabled days, a single date or a range
/// (with a preview of the range under the pointer). Used by the date pickers, usable on its own.
/// </summary>
[TemplatePart("PART_Days", typeof(Panel))]
[TemplatePart("PART_Previous", typeof(Button))]
[TemplatePart("PART_Next", typeof(Button))]
public class HelixCalendar : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixCalendar>();
    public static readonly StyledProperty<global::Avalonia.Media.IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixCalendar>();
    public static readonly StyledProperty<DateTime?> SelectedDateProperty = AvaloniaProperty.Register<HelixCalendar, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<DateTime?> RangeStartProperty = AvaloniaProperty.Register<HelixCalendar, DateTime?>(nameof(RangeStart));
    public static readonly StyledProperty<DateTime?> RangeEndProperty = AvaloniaProperty.Register<HelixCalendar, DateTime?>(nameof(RangeEnd));
    public static readonly StyledProperty<bool> PreviewRangeProperty = AvaloniaProperty.Register<HelixCalendar, bool>(nameof(PreviewRange));
    public static readonly StyledProperty<DateTime> DisplayDateProperty = AvaloniaProperty.Register<HelixCalendar, DateTime>(nameof(DisplayDate), DateTime.Today);
    public static readonly StyledProperty<DateTime?> MinimumProperty = AvaloniaProperty.Register<HelixCalendar, DateTime?>(nameof(Minimum));
    public static readonly StyledProperty<DateTime?> MaximumProperty = AvaloniaProperty.Register<HelixCalendar, DateTime?>(nameof(Maximum));
    public static readonly StyledProperty<Func<DateTime, bool>?> IsDateDisabledProperty = AvaloniaProperty.Register<HelixCalendar, Func<DateTime, bool>?>(nameof(IsDateDisabled));
    public static readonly StyledProperty<CultureInfo?> CultureProperty = AvaloniaProperty.Register<HelixCalendar, CultureInfo?>(nameof(Culture));
    public static readonly DirectProperty<HelixCalendar, string> MonthTextProperty = AvaloniaProperty.RegisterDirect<HelixCalendar, string>(nameof(MonthText), c => c.MonthText);
    public static readonly RoutedEvent<HelixEventArgs<DateTime>> DatePickedEvent = RoutedEvent.Register<HelixCalendar, HelixEventArgs<DateTime>>(nameof(DatePicked), RoutingStrategies.Bubble);

    private Panel? _days;
    private Button? _previous, _next;
    private string _monthText = "";
    private DateTime? _hover;

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public global::Avalonia.Media.IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>The chosen day (single mode).</summary>
    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    /// <summary>Range mode: the first day (drawn as selected).</summary>
    public DateTime? RangeStart { get => GetValue(RangeStartProperty); set => SetValue(RangeStartProperty, value); }

    /// <summary>Range mode: the last day.</summary>
    public DateTime? RangeEnd { get => GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, value); }

    /// <summary>Range mode, after the first click: the range follows the pointer.</summary>
    public bool PreviewRange { get => GetValue(PreviewRangeProperty); set => SetValue(PreviewRangeProperty, value); }

    /// <summary>The month shown.</summary>
    public DateTime DisplayDate { get => GetValue(DisplayDateProperty); set => SetValue(DisplayDateProperty, value); }
    public DateTime? Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public DateTime? Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Days that cannot be picked, e.g. d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday.</summary>
    public Func<DateTime, bool>? IsDateDisabled { get => GetValue(IsDateDisabledProperty); set => SetValue(IsDateDisabledProperty, value); }
    public CultureInfo? Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>"November 2026".</summary>
    public string MonthText { get => _monthText; private set => SetAndRaise(MonthTextProperty, ref _monthText, value); }

    /// <summary>Raised when a day is clicked (or chosen with Enter).</summary>
    public event EventHandler<HelixEventArgs<DateTime>>? DatePicked { add => AddHandler(DatePickedEvent, value); remove => RemoveHandler(DatePickedEvent, value); }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_previous is not null) _previous.Click -= OnPrevious;
        if (_next is not null) _next.Click -= OnNext;
        base.OnApplyTemplate(e);
        _days = e.NameScope.Find<Panel>("PART_Days");
        _previous = e.NameScope.Find<Button>("PART_Previous");
        _next = e.NameScope.Find<Button>("PART_Next");
        if (_previous is not null) { _previous.Click += OnPrevious; ToolTip.SetTip(_previous, HelixTexts.Current.Previous); }
        if (_next is not null) { _next.Click += OnNext; ToolTip.SetTip(_next, HelixTexts.Current.Next); }
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixCalendar) && change.Property != MonthTextProperty) Build();
    }

    private void OnPrevious(object? sender, RoutedEventArgs e) => SetCurrentValue(DisplayDateProperty, Month.AddMonths(-1));
    private void OnNext(object? sender, RoutedEventArgs e) => SetCurrentValue(DisplayDateProperty, Month.AddMonths(1));

    private DateTime Month => new(DisplayDate.Year, DisplayDate.Month, 1);

    /// <summary>A day outside Minimum / Maximum or refused by IsDateDisabled.</summary>
    public bool IsDisabled(DateTime day) =>
        (Minimum is { } min && day.Date < min.Date) || (Maximum is { } max && day.Date > max.Date) || IsDateDisabled?.Invoke(day.Date) == true;

    private void Build()
    {
        var culture = EffectiveCulture;
        var month = Month;
        MonthText = culture.TextInfo.ToTitleCase(month.ToString("MMMM yyyy", culture));
        if (_previous is not null) _previous.IsEnabled = Minimum is not { } min || month > new DateTime(min.Year, min.Month, 1);
        if (_next is not null) _next.IsEnabled = Maximum is not { } max || month < new DateTime(max.Year, max.Month, 1);
        if (_days is null) return;
        _days.Children.Clear();

        var first = culture.DateTimeFormat.FirstDayOfWeek;
        for (var i = 0; i < 7; i++)
        {
            var name = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)(((int)first + i) % 7));
            var label = new TextBlock { Text = name, FontSize = 11.5, FontWeight = global::Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 4) };
            label.Bind(TextBlock.ForegroundProperty, label.GetResourceObservable(HelixKeys.TextSubtle));
            _days.Children.Add(label);
        }

        var offset = ((int)month.DayOfWeek - (int)first + 7) % 7;
        var start = month.AddDays(-offset);
        var (rangeFrom, rangeTo) = RangeBounds();
        for (var i = 0; i < 42; i++)
        {
            var day = start.AddDays(i);
            var button = new Button { Content = day.Day.ToString(culture), Tag = day };
            if (this.TryFindResource("HelixCalendarDay", out var theme) && theme is global::Avalonia.Styling.ControlTheme dayTheme) button.Theme = dayTheme;
            button.Classes.Set("outside", day.Month != month.Month);
            button.Classes.Set("today", day.Date == DateTime.Today);
            button.Classes.Set("selected", day.Date == SelectedDate?.Date || day.Date == rangeFrom || day.Date == rangeTo);
            button.Classes.Set("in-range", rangeFrom is { } a && rangeTo is { } b && day.Date > a && day.Date < b);
            button.IsEnabled = !IsDisabled(day);
            button.Click += (_, _) => Pick(day);
            button.PointerEntered += (_, _) => { if (PreviewRange) { _hover = day; Build(); } };
            _days.Children.Add(button);
        }
    }

    private (DateTime? From, DateTime? To) RangeBounds()
    {
        var start = RangeStart?.Date;
        var end = RangeEnd?.Date ?? (PreviewRange ? _hover?.Date : null);
        if (start is null) return (null, null);
        if (end is null) return (start, start);
        return end < start ? (end, start) : (start, end);
    }

    private void Pick(DateTime day)
    {
        if (IsDisabled(day)) return;
        if (day.Month != DisplayDate.Month || day.Year != DisplayDate.Year) SetCurrentValue(DisplayDateProperty, day);
        RaiseEvent(new HelixEventArgs<DateTime>(DatePickedEvent, day.Date));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var current = SelectedDate ?? RangeEnd ?? RangeStart ?? DisplayDate;
        var target = e.Key switch
        {
            Key.Left => current.AddDays(-1),
            Key.Right => current.AddDays(1),
            Key.Up => current.AddDays(-7),
            Key.Down => current.AddDays(7),
            Key.PageUp => current.AddMonths(-1),
            Key.PageDown => current.AddMonths(1),
            _ => (DateTime?)null,
        };
        if (target is { } t && !IsDisabled(t))
        {
            SetCurrentValue(DisplayDateProperty, t);
            if (RangeStart is null && !PreviewRange) SetCurrentValue(SelectedDateProperty, t.Date);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            Pick(current);
            e.Handled = true;
        }
    }
}

/// <summary>Field shell, popup and clear button shared by the date pickers.</summary>
[TemplatePart("PART_Calendar", typeof(HelixCalendar))]
[TemplatePart("PART_Clear", typeof(Button))]
public abstract class HelixPickerBase : HelixFieldBase
{
    public static readonly StyledProperty<bool> IsDropDownOpenProperty = AvaloniaProperty.Register<HelixPickerBase, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> IsClearableProperty = AvaloniaProperty.Register<HelixPickerBase, bool>(nameof(IsClearable), true);
    public static readonly StyledProperty<DateTime?> MinimumProperty = AvaloniaProperty.Register<HelixPickerBase, DateTime?>(nameof(Minimum));
    public static readonly StyledProperty<DateTime?> MaximumProperty = AvaloniaProperty.Register<HelixPickerBase, DateTime?>(nameof(Maximum));
    public static readonly StyledProperty<Func<DateTime, bool>?> IsDateDisabledProperty = AvaloniaProperty.Register<HelixPickerBase, Func<DateTime, bool>?>(nameof(IsDateDisabled));
    public static readonly DirectProperty<HelixPickerBase, string?> DisplayTextProperty = AvaloniaProperty.RegisterDirect<HelixPickerBase, string?>(nameof(DisplayText), p => p.DisplayText);
    public static readonly DirectProperty<HelixPickerBase, bool> HasValueProperty = AvaloniaProperty.RegisterDirect<HelixPickerBase, bool>(nameof(HasValue), p => p.HasValue);

    private string? _displayText;
    private bool _hasValue;
    private Button? _clear;

    protected HelixPickerBase() => SetCurrentValue(IconProperty, "calendar");

    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>An × that empties the field (true).</summary>
    public bool IsClearable { get => GetValue(IsClearableProperty); set => SetValue(IsClearableProperty, value); }
    public DateTime? Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public DateTime? Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Days that cannot be picked, e.g. weekends.</summary>
    public Func<DateTime, bool>? IsDateDisabled { get => GetValue(IsDateDisabledProperty); set => SetValue(IsDateDisabledProperty, value); }

    /// <summary>The value as shown in the field.</summary>
    public string? DisplayText { get => _displayText; protected set => SetAndRaise(DisplayTextProperty, ref _displayText, value); }
    public bool HasValue { get => _hasValue; protected set => SetAndRaise(HasValueProperty, ref _hasValue, value); }

    public static string TodayText => HelixTexts.Current.Today;
    public static string NowText => HelixTexts.Current.Now;
    public static string ClearText => HelixTexts.Current.Clear;

    protected HelixCalendar? CalendarPart { get; private set; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (CalendarPart is not null) CalendarPart.DatePicked -= OnDatePicked;
        if (_clear is not null) _clear.Click -= OnClear;
        base.OnApplyTemplate(e);
        CalendarPart = e.NameScope.Find<HelixCalendar>("PART_Calendar");
        _clear = e.NameScope.Find<Button>("PART_Clear");
        if (CalendarPart is not null) CalendarPart.DatePicked += OnDatePicked;
        if (_clear is not null) { _clear.Click += OnClear; ToolTip.SetTip(_clear, ClearText); }
        OnTemplateReady(e);
        Sync();
    }

    protected virtual void OnTemplateReady(TemplateAppliedEventArgs e)
    {
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDropDownOpenProperty && IsDropDownOpen)
        {
            OnOpening();
            Sync();
            Dispatcher.UIThread.Post(() => CalendarPart?.Focus(), DispatcherPriority.Input);
        }
        else if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == IsDateDisabledProperty || change.Property == CultureProperty)
        {
            Sync();
        }

        if (change.Property == HasValueProperty || change.Property == IsClearableProperty) PseudoClasses.Set(":clearable", HasValue && IsClearable);
    }

    private void OnDatePicked(object? sender, HelixEventArgs<DateTime> e) => OnPicked(e.Item);

    private void OnClear(object? sender, RoutedEventArgs e) => ClearValue();

    protected void Close() => SetCurrentValue(IsDropDownOpenProperty, false);

    /// <summary>Copies the bounds and the value to the calendar.</summary>
    protected virtual void Sync()
    {
        if (CalendarPart is null) return;
        CalendarPart.Minimum = Minimum;
        CalendarPart.Maximum = Maximum;
        CalendarPart.IsDateDisabled = IsDateDisabled;
        CalendarPart.Culture = Culture;
    }

    protected virtual void OnOpening()
    {
    }

    protected abstract void OnPicked(DateTime day);

    protected abstract void ClearValue();
}

/// <summary>A date in a field with the Helix calendar: Label, Hint, Error, Minimum / Maximum, disabled days, Format.</summary>
[TemplatePart("PART_Today", typeof(Button))]
public class HelixDatePicker : HelixPickerBase
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty = AvaloniaProperty.Register<HelixDatePicker, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<HelixDatePicker, string>(nameof(Format), "d MMM yyyy");

    private Button? _today;

    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    /// <summary>.NET date format of the field ("d MMM yyyy").</summary>
    public string Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    protected override void OnTemplateReady(TemplateAppliedEventArgs e)
    {
        if (_today is not null) _today.Click -= OnToday;
        _today = e.NameScope.Find<Button>("PART_Today");
        if (_today is not null) _today.Click += OnToday;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateProperty || change.Property == FormatProperty || change.Property == CultureProperty)
        {
            DisplayText = SelectedDate?.ToString(Format, EffectiveCulture);
            HasValue = SelectedDate is not null;
            Sync();
        }
    }

    private void OnToday(object? sender, RoutedEventArgs e) => OnPicked(DateTime.Today);

    protected override void Sync()
    {
        base.Sync();
        if (CalendarPart is null) return;
        CalendarPart.SelectedDate = SelectedDate;
        CalendarPart.DisplayDate = SelectedDate ?? DateTime.Today;
        if (_today is not null) _today.IsEnabled = CalendarPart.IsDisabled(DateTime.Today) is false;
    }

    protected override void OnPicked(DateTime day)
    {
        SetCurrentValue(SelectedDateProperty, day);
        Close();
    }

    protected override void ClearValue() => SetCurrentValue(SelectedDateProperty, null);
}

/// <summary>A date and a time in one field: the calendar, then a list of times every MinuteStep minutes (15 by default).</summary>
[TemplatePart("PART_Times", typeof(ListBox))]
[TemplatePart("PART_Now", typeof(Button))]
public class HelixDateTimePicker : HelixPickerBase
{
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty = AvaloniaProperty.Register<HelixDateTimePicker, DateTime?>(nameof(SelectedDateTime), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int> MinuteStepProperty = AvaloniaProperty.Register<HelixDateTimePicker, int>(nameof(MinuteStep), 15);
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<HelixDateTimePicker, string?>(nameof(Format));

    private ListBox? _times;
    private Button? _now;
    private DateTime? _pendingDate;
    private bool _syncing;

    public DateTime? SelectedDateTime { get => GetValue(SelectedDateTimeProperty); set => SetValue(SelectedDateTimeProperty, value); }

    /// <summary>Minutes between two times of the list (15: quarter hours).</summary>
    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    /// <summary>.NET format of the field ("d MMM yyyy  HH:mm" style by default).</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    private int Step => Math.Clamp(MinuteStep, 1, 720);

    protected override void OnTemplateReady(TemplateAppliedEventArgs e)
    {
        if (_times is not null) _times.SelectionChanged -= OnTimePicked;
        if (_now is not null) _now.Click -= OnNow;
        _times = e.NameScope.Find<ListBox>("PART_Times");
        _now = e.NameScope.Find<Button>("PART_Now");
        if (_times is not null) _times.SelectionChanged += OnTimePicked;
        if (_now is not null) _now.Click += OnNow;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateTimeProperty || change.Property == FormatProperty || change.Property == CultureProperty || change.Property == MinuteStepProperty)
        {
            var culture = EffectiveCulture;
            DisplayText = SelectedDateTime is { } v
                ? Format is { Length: > 0 } f ? v.ToString(f, culture) : $"{v.ToString("d MMM yyyy", culture)}  {v.ToString("t", culture)}"
                : null;
            HasValue = SelectedDateTime is not null;
            if (!_syncing) Sync();
        }
    }

    private DateTime? SelectedDay => _pendingDate ?? SelectedDateTime?.Date;

    private TimeSpan Snap(TimeSpan time) => TimeSpan.FromMinutes((int)time.TotalMinutes - (int)time.TotalMinutes % Step);

    protected override void OnOpening() => _pendingDate = null;

    protected override void Sync()
    {
        base.Sync();
        if (CalendarPart is not null)
        {
            CalendarPart.SelectedDate = SelectedDay;
            CalendarPart.DisplayDate = SelectedDay ?? DateTime.Today;
        }

        if (_times is null) return;
        var culture = EffectiveCulture;
        var date = SelectedDay;
        var slots = Enumerable.Range(0, 24 * 60 / Step).Select(i =>
        {
            var time = TimeSpan.FromMinutes(i * Step);
            var moment = date?.Add(time);
            var enabled = moment is null || ((Minimum is not { } min || moment >= min) && (Maximum is not { } max || moment <= max));
            return new HelixTimeSlot(time, DateTime.Today.Add(time).ToString("t", culture), enabled);
        }).ToList();
        _syncing = true;
        _times.ItemsSource = slots;
        var selected = SelectedDateTime is { } value ? slots.FirstOrDefault(s => s.Time == Snap(value.TimeOfDay)) : null;
        _times.SelectedItem = selected;
        _syncing = false;
        var target = selected ?? slots.FirstOrDefault(s => s.Time == TimeSpan.FromHours(8)) ?? slots.FirstOrDefault();
        if (target is not null) Dispatcher.UIThread.Post(() => _times?.ScrollIntoView(target), DispatcherPriority.Loaded);
    }

    protected override void OnPicked(DateTime day)
    {
        // Keeps the time already chosen; the panel stays open for the time.
        var moment = day.Date + (SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero);
        if (Minimum is { } min && moment < min) moment = min;
        if (Maximum is { } max && moment > max) moment = max;
        _pendingDate = day.Date;
        SetCurrentValue(SelectedDateTimeProperty, moment);
    }

    private void OnTimePicked(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing || _times?.SelectedItem is not HelixTimeSlot slot) return;
        if (!slot.IsEnabled) return;
        SetCurrentValue(SelectedDateTimeProperty, (SelectedDay ?? DateTime.Today).Add(slot.Time));
        Close();
    }

    private void OnNow(object? sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        SetCurrentValue(SelectedDateTimeProperty, now.Date.Add(Snap(now.TimeOfDay)));
        Close();
    }

    protected override void ClearValue() => SetCurrentValue(SelectedDateTimeProperty, null);
}

/// <summary>
/// A period of whole days in one field: shortcuts on the left ("Last 7 days"…), the calendar on the right
/// (click the first day, then the last). The value is a <see cref="HelixDateRange"/>.
/// </summary>
[TemplatePart("PART_Presets", typeof(Panel))]
public class HelixDateRangePicker : HelixPickerBase
{
    public static readonly StyledProperty<HelixDateRange?> SelectedRangeProperty = AvaloniaProperty.Register<HelixDateRangePicker, HelixDateRange?>(nameof(SelectedRange), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int?> MaxDaysProperty = AvaloniaProperty.Register<HelixDateRangePicker, int?>(nameof(MaxDays));
    public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<HelixDateRangePicker, string>(nameof(Format), "d MMM yyyy");
    public static readonly StyledProperty<bool> ShowPresetsProperty = AvaloniaProperty.Register<HelixDateRangePicker, bool>(nameof(ShowPresets), true);
    public static readonly StyledProperty<IReadOnlyList<HelixDateRangePreset>?> PresetsProperty = AvaloniaProperty.Register<HelixDateRangePicker, IReadOnlyList<HelixDateRangePreset>?>(nameof(Presets));
    public static readonly DirectProperty<HelixDateRangePicker, bool> IsPickingEndProperty = AvaloniaProperty.RegisterDirect<HelixDateRangePicker, bool>(nameof(IsPickingEnd), p => p.IsPickingEnd);

    private Panel? _presets;
    private DateTime? _start;
    private bool _isPickingEnd;

    public HelixDateRange? SelectedRange { get => GetValue(SelectedRangeProperty); set => SetValue(SelectedRangeProperty, value); }

    /// <summary>Longest period allowed, in days.</summary>
    public int? MaxDays { get => GetValue(MaxDaysProperty); set => SetValue(MaxDaysProperty, value); }
    public string Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The shortcuts on the left (true).</summary>
    public bool ShowPresets { get => GetValue(ShowPresetsProperty); set => SetValue(ShowPresetsProperty, value); }

    /// <summary>Your own shortcuts (else today, yesterday, last 7 / 30 days, this week, this month, last month).</summary>
    public IReadOnlyList<HelixDateRangePreset>? Presets { get => GetValue(PresetsProperty); set => SetValue(PresetsProperty, value); }

    /// <summary>True between the first and the second click: the panel asks for the last day.</summary>
    public bool IsPickingEnd { get => _isPickingEnd; private set => SetAndRaise(IsPickingEndProperty, ref _isPickingEnd, value); }

    public static string PickEndText => HelixTexts.Current.PickEndDate;
    public static string PeriodText => HelixTexts.Current.ChoosePeriod;

    protected override void OnTemplateReady(TemplateAppliedEventArgs e)
    {
        _presets = e.NameScope.Find<Panel>("PART_Presets");
        BuildPresets();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedRangeProperty || change.Property == FormatProperty || change.Property == CultureProperty)
        {
            var culture = EffectiveCulture;
            DisplayText = SelectedRange is { } r
                ? r.Days == 1 ? r.Start.ToString(Format, culture) : $"{r.Start.ToString(Format, culture)} – {r.End.ToString(Format, culture)}"
                : null;
            HasValue = SelectedRange is not null;
            Sync();
        }
        else if (change.Property == PresetsProperty || change.Property == CultureProperty)
        {
            BuildPresets();
        }
    }

    private IReadOnlyList<HelixDateRangePreset> EffectivePresets()
    {
        if (Presets is { } presets) return presets;
        var texts = HelixTexts.Current;
        var firstDay = EffectiveCulture.DateTimeFormat.FirstDayOfWeek;
        return
        [
            new(texts.Today, t => new(t, t)),
            new(texts.Yesterday, t => new(t.AddDays(-1), t.AddDays(-1))),
            new(texts.Last7Days, t => new(t.AddDays(-6), t)),
            new(texts.Last30Days, t => new(t.AddDays(-29), t)),
            new(texts.ThisWeek, t => new(t.AddDays(-(((int)t.DayOfWeek - (int)firstDay + 7) % 7)), t)),
            new(texts.ThisMonth, t => new(new DateTime(t.Year, t.Month, 1), t)),
            new(texts.LastMonth, t =>
            {
                var first = new DateTime(t.Year, t.Month, 1).AddMonths(-1);
                return new(first, first.AddMonths(1).AddDays(-1));
            }),
        ];
    }

    private void BuildPresets()
    {
        if (_presets is null) return;
        _presets.Children.Clear();
        foreach (var preset in EffectivePresets())
        {
            var button = new HelixButton
            {
                Content = preset.Label, Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm,
                HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, FontWeight = global::Avalonia.Media.FontWeight.Normal,
            };
            var p = preset;
            button.Click += (_, _) =>
            {
                SetCurrentValue(SelectedRangeProperty, p.Range(DateTime.Today));
                Close();
            };
            _presets.Children.Add(button);
        }
    }

    protected override void OnOpening()
    {
        _start = null;
        IsPickingEnd = false;
    }

    protected override void Sync()
    {
        base.Sync();
        if (CalendarPart is null) return;
        CalendarPart.PreviewRange = _start is not null;
        CalendarPart.RangeStart = _start ?? SelectedRange?.Start;
        CalendarPart.RangeEnd = _start is null ? SelectedRange?.End : null;
        if (_start is { } s && MaxDays is { } max and > 0)
        {
            var from = s.AddDays(-(max - 1));
            var to = s.AddDays(max - 1);
            CalendarPart.Minimum = Minimum is { } min && min.Date > from ? min.Date : from;
            CalendarPart.Maximum = Maximum is { } mx && mx.Date < to ? mx.Date : to;
        }

        if (_start is null) CalendarPart.DisplayDate = SelectedRange?.End ?? DateTime.Today;
    }

    protected override void OnPicked(DateTime day)
    {
        if (_start is not { } start)
        {
            // First click: the start. Days beyond MaxDays become unreachable.
            _start = day;
            IsPickingEnd = true;
            Sync();
            return;
        }

        _start = null;
        IsPickingEnd = false;
        SetCurrentValue(SelectedRangeProperty, new HelixDateRange(start, day));
        Close();
    }

    protected override void ClearValue() => SetCurrentValue(SelectedRangeProperty, null);
}
