using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Calendar = System.Windows.Controls.Calendar;

namespace Helix.Wpf;

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

/// <summary>Shared parts of the date pickers that open a panel: field shell, accent, popup, culture.</summary>
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_Calendar", Type = typeof(Calendar))]
[TemplatePart(Name = "PART_Clear", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Chrome", Type = typeof(FrameworkElement))]
public abstract class HelixPanelPicker : Control
{
    protected HelixPanelPicker() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixPanelPicker));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixPanelPicker));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixPanelPicker));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixPanelPicker));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixPanelPicker));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixPanelPicker));

    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixPanelPicker, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixPanelPicker, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixPanelPicker, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixPanelPicker), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixPanelPicker, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixPanelPicker, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty IsClearableProperty = HelixProperty.Register<HelixPanelPicker, bool>(nameof(IsClearable), true);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixPanelPicker, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(HelixPanelPicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((HelixPanelPicker)d).OnDropDownChanged((bool)e.NewValue)));

    private static readonly DependencyPropertyKey DisplayTextKey = DependencyProperty.RegisterReadOnly(nameof(DisplayText), typeof(string), typeof(HelixPanelPicker), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasValueKey = DependencyProperty.RegisterReadOnly(nameof(HasValue), typeof(bool), typeof(HelixPanelPicker), new PropertyMetadata(false));
    public static readonly DependencyProperty DisplayTextProperty = DisplayTextKey.DependencyProperty;
    public static readonly DependencyProperty HasValueProperty = HasValueKey.DependencyProperty;

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (focus ring and selection).</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The × button that empties the value (true).</summary>
    public bool IsClearable { get => (bool)GetValue(IsClearableProperty); set => SetValue(IsClearableProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>The value as shown in the field (null when empty).</summary>
    public string? DisplayText => (string?)GetValue(DisplayTextProperty);
    public bool HasValue => (bool)GetValue(HasValueProperty);

    public static string ClearText => HelixTexts.Current.Clear;

    protected Calendar? CalendarPart { get; private set; }
    private Popup? _popup;
    private ButtonBase? _clear;
    private FrameworkElement? _chrome;

    protected static CultureInfo Culture => CultureInfo.CurrentCulture;

    public override void OnApplyTemplate()
    {
        if (CalendarPart is not null)
        {
            CalendarPart.SelectedDatesChanged -= OnCalendarChanged;
            CalendarPart.PreviewMouseUp -= ReleaseCalendarCapture;
        }

        if (_clear is not null) _clear.Click -= OnClear;
        if (_chrome is not null) _chrome.MouseLeftButtonUp -= OnChromeClick;
        base.OnApplyTemplate();
        _chrome = GetTemplateChild("PART_Chrome") as FrameworkElement;
        if (_chrome is not null) _chrome.MouseLeftButtonUp += OnChromeClick;
        CalendarPart = GetTemplateChild("PART_Calendar") as Calendar;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _clear = GetTemplateChild("PART_Clear") as ButtonBase;
        if (CalendarPart is not null)
        {
            CalendarPart.SelectedDatesChanged += OnCalendarChanged;
            CalendarPart.PreviewMouseUp += ReleaseCalendarCapture;
        }

        if (_clear is not null) _clear.Click += OnClear;
        if (_popup is not null) _popup.Closed += (_, _) => SetCurrentValue(IsDropDownOpenProperty, false);
        SyncCalendar();
    }

    /// <summary>The WPF calendar keeps the mouse captured after a click; without this, the next click elsewhere is lost.</summary>
    private static void ReleaseCalendarCapture(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.Captured is CalendarItem or CalendarDayButton) Mouse.Capture(null);
    }

    private void OnClear(object sender, RoutedEventArgs e) => ClearValue();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            Focus();
            e.Handled = true;
        }
        else if (!IsDropDownOpen && (e.Key is Key.Enter or Key.Space || (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Alt)))
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
    }

    /// <summary>A click on the field (not on its × button, which handles its own click) opens or closes the panel.</summary>
    private void OnChromeClick(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled) return;
        SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
        Focus();
        e.Handled = true;
    }

    private void OnDropDownChanged(bool open)
    {
        if (open)
        {
            OnOpening();
            SyncCalendar();
            Dispatcher.BeginInvoke(() => CalendarPart?.Focus(), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    protected void SetDisplay(string? text)
    {
        SetValue(DisplayTextKey, text);
        SetValue(HasValueKey, text is not null);
    }

    protected bool Syncing { get; set; }

    protected void Close()
    {
        SetCurrentValue(IsDropDownOpenProperty, false);
        Focus();
    }

    private void OnCalendarChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!Syncing) OnCalendarPicked();
    }

    /// <summary>Called before the panel opens.</summary>
    protected virtual void OnOpening()
    {
    }

    /// <summary>Puts the current value into the calendar.</summary>
    protected abstract void SyncCalendar();

    /// <summary>The user chose a day (or the end of a range) in the calendar.</summary>
    protected abstract void OnCalendarPicked();

    /// <summary>Empties the value (× button).</summary>
    protected abstract void ClearValue();
}

/// <summary>
/// A date and a time in one field: the Helix calendar and a list of times at <see cref="MinuteStep"/> (15 minutes by default).
/// Choosing a day keeps the time already set; choosing a time closes the panel.
/// </summary>
[TemplatePart(Name = "PART_Times", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_Now", Type = typeof(ButtonBase))]
public class HelixDateTimePicker : HelixPanelPicker
{
    static HelixDateTimePicker() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDateTimePicker), new FrameworkPropertyMetadata(typeof(HelixDateTimePicker)));

    public HelixDateTimePicker() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.ChooseDateTime);

    public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime?), typeof(HelixDateTimePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDateTimePicker)d).OnValueChanged()));
    public static readonly DependencyProperty MinimumProperty = HelixProperty.Register<HelixDateTimePicker, DateTime?>(nameof(Minimum), null, (d, _) => ((HelixDateTimePicker)d).SyncCalendar());
    public static readonly DependencyProperty MaximumProperty = HelixProperty.Register<HelixDateTimePicker, DateTime?>(nameof(Maximum), null, (d, _) => ((HelixDateTimePicker)d).SyncCalendar());
    public static readonly DependencyProperty MinuteStepProperty = HelixProperty.Register<HelixDateTimePicker, int>(nameof(MinuteStep), 15, (d, _) => ((HelixDateTimePicker)d).BuildTimes());
    public static readonly DependencyProperty FormatProperty = HelixProperty.Register<HelixDateTimePicker, string?>(nameof(Format), null, (d, _) => ((HelixDateTimePicker)d).OnValueChanged());

    /// <summary>The chosen moment, two-way.</summary>
    public DateTime? SelectedDateTime { get => (DateTime?)GetValue(SelectedDateTimeProperty); set => SetValue(SelectedDateTimeProperty, value); }

    /// <summary>Earliest allowed moment: days and times before are disabled.</summary>
    public DateTime? Minimum { get => (DateTime?)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>Latest allowed moment.</summary>
    public DateTime? Maximum { get => (DateTime?)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Minutes between two times of the list: 15 for quarter hours, 60 for hours, 1 for any minute.</summary>
    public int MinuteStep { get => (int)GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    /// <summary>Display format; null shows "d MMM yyyy" and the culture's short time.</summary>
    public string? Format { get => (string?)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    public static string NowText => HelixTexts.Current.Now;
    public static string TimeText => HelixTexts.Current.ChooseTime;

    private ListBox? _times;
    private ButtonBase? _now;
    private DateTime? _pendingDate;

    private int Step => Math.Clamp(MinuteStep, 1, 720);

    public override void OnApplyTemplate()
    {
        if (_times is not null) _times.SelectionChanged -= OnTimePicked;
        if (_now is not null) _now.Click -= OnNow;
        base.OnApplyTemplate();
        _times = GetTemplateChild("PART_Times") as ListBox;
        _now = GetTemplateChild("PART_Now") as ButtonBase;
        if (_times is not null) _times.SelectionChanged += OnTimePicked;
        if (_now is not null) _now.Click += OnNow;
        BuildTimes();
        OnValueChanged();
    }

    private DateTime? SelectedDate => _pendingDate ?? SelectedDateTime?.Date;

    private void OnValueChanged()
    {
        SetDisplay(SelectedDateTime is { } value
            ? Format is { Length: > 0 } format ? value.ToString(format, Culture) : $"{value.ToString("d MMM yyyy", Culture)}  {value.ToString("t", Culture)}"
            : null);
        if (!Syncing) BuildTimes();
    }

    private TimeSpan Snap(TimeSpan time) => TimeSpan.FromMinutes((int)time.TotalMinutes - (int)time.TotalMinutes % Step);

    private void BuildTimes()
    {
        if (_times is null) return;
        var date = SelectedDate;
        var slots = Enumerable.Range(0, 24 * 60 / Step).Select(i =>
        {
            var time = TimeSpan.FromMinutes(i * Step);
            var moment = date?.Add(time);
            var enabled = moment is null || ((Minimum is not { } min || moment >= min) && (Maximum is not { } max || moment <= max));
            return new HelixTimeSlot(time, DateTime.Today.Add(time).ToString("t", Culture), enabled);
        }).ToList();

        Syncing = true;
        _times.ItemsSource = slots;
        var selected = SelectedDateTime is { } value ? slots.FirstOrDefault(s => s.Time == Snap(value.TimeOfDay)) : null;
        _times.SelectedItem = selected;
        Syncing = false;
        _times.ScrollIntoView(selected ?? slots.FirstOrDefault(s => s.Time == TimeSpan.FromHours(8)) ?? slots.FirstOrDefault());
    }

    protected override void OnOpening() => _pendingDate = null;

    protected override void SyncCalendar()
    {
        if (CalendarPart is null) return;
        Syncing = true;
        CalendarPart.DisplayDateStart = Minimum?.Date;
        CalendarPart.DisplayDateEnd = Maximum?.Date;
        CalendarPart.SelectedDate = SelectedDateTime?.Date;
        if (SelectedDateTime is { } value) CalendarPart.DisplayDate = value.Date;
        Syncing = false;
        BuildTimes();
    }

    protected override void OnCalendarPicked()
    {
        if (CalendarPart?.SelectedDate is not { } date) return;

        // Keeps the time already chosen; the panel stays open for the time.
        var moment = date.Date + (SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero);
        if (Minimum is { } min && moment < min) moment = min;
        if (Maximum is { } max && moment > max) moment = max;
        _pendingDate = date.Date;
        Syncing = true;
        SetCurrentValue(SelectedDateTimeProperty, moment);
        Syncing = false;
        BuildTimes();
        Dispatcher.BeginInvoke(() => (_times?.ItemContainerGenerator.ContainerFromItem(_times.SelectedItem) as UIElement ?? _times)?.Focus(),
            System.Windows.Threading.DispatcherPriority.Input);
    }

    private void OnTimePicked(object sender, SelectionChangedEventArgs e)
    {
        if (Syncing || _times?.SelectedItem is not HelixTimeSlot slot) return;
        var date = SelectedDate ?? DateTime.Today;
        SetCurrentValue(SelectedDateTimeProperty, date.Add(slot.Time));
        Close();
    }

    private void OnNow(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        SetCurrentValue(SelectedDateTimeProperty, now.Date.Add(Snap(now.TimeOfDay)));
        Close();
    }

    protected override void ClearValue() => SetCurrentValue(SelectedDateTimeProperty, null);
}

/// <summary>
/// A period of whole days in one field: shortcuts on the left ("Last 7 days"…), the Helix calendar on the right
/// (click the first day, then the last). The value is a <see cref="HelixDateRange"/>.
/// </summary>
[TemplatePart(Name = "PART_Presets", Type = typeof(ItemsControl))]
public class HelixDateRangePicker : HelixPanelPicker
{
    static HelixDateRangePicker() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDateRangePicker), new FrameworkPropertyMetadata(typeof(HelixDateRangePicker)));

    public HelixDateRangePicker() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.ChoosePeriod);

    public static readonly DependencyProperty SelectedRangeProperty = DependencyProperty.Register(nameof(SelectedRange), typeof(HelixDateRange), typeof(HelixDateRangePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDateRangePicker)d).OnValueChanged()));
    public static readonly DependencyProperty MinimumProperty = HelixProperty.Register<HelixDateRangePicker, DateTime?>(nameof(Minimum), null);
    public static readonly DependencyProperty MaximumProperty = HelixProperty.Register<HelixDateRangePicker, DateTime?>(nameof(Maximum), null);
    public static readonly DependencyProperty MaxDaysProperty = HelixProperty.Register<HelixDateRangePicker, int?>(nameof(MaxDays), null);
    public static readonly DependencyProperty FormatProperty = HelixProperty.Register<HelixDateRangePicker, string>(nameof(Format), "d MMM yyyy", (d, _) => ((HelixDateRangePicker)d).OnValueChanged());
    public static readonly DependencyProperty ShowPresetsProperty = HelixProperty.Register<HelixDateRangePicker, bool>(nameof(ShowPresets), true);
    public static readonly DependencyProperty PresetsProperty = HelixProperty.Register<HelixDateRangePicker, IReadOnlyList<HelixDateRangePreset>?>(nameof(Presets), null, (d, _) => ((HelixDateRangePicker)d).BuildPresets());

    private static readonly DependencyPropertyKey IsPickingEndKey = DependencyProperty.RegisterReadOnly(nameof(IsPickingEnd), typeof(bool), typeof(HelixDateRangePicker), new PropertyMetadata(false));
    public static readonly DependencyProperty IsPickingEndProperty = IsPickingEndKey.DependencyProperty;

    /// <summary>The chosen period (Start, End, both included), two-way.</summary>
    public HelixDateRange? SelectedRange { get => (HelixDateRange?)GetValue(SelectedRangeProperty); set => SetValue(SelectedRangeProperty, value); }
    public DateTime? Minimum { get => (DateTime?)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public DateTime? Maximum { get => (DateTime?)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Longest period allowed, in days (both ends included).</summary>
    public int? MaxDays { get => (int?)GetValue(MaxDaysProperty); set => SetValue(MaxDaysProperty, value); }

    /// <summary>Format of each day in the field.</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The shortcuts on the left (true).</summary>
    public bool ShowPresets { get => (bool)GetValue(ShowPresetsProperty); set => SetValue(ShowPresetsProperty, value); }

    /// <summary>Your shortcuts; null: today, yesterday, last 7 / 30 days, this week, this month, last month.</summary>
    public IReadOnlyList<HelixDateRangePreset>? Presets { get => (IReadOnlyList<HelixDateRangePreset>?)GetValue(PresetsProperty); set => SetValue(PresetsProperty, value); }

    /// <summary>True between the first and the second click: the panel asks for the last day.</summary>
    public bool IsPickingEnd => (bool)GetValue(IsPickingEndProperty);

    public static string PickEndText => HelixTexts.Current.PickEndDate;
    public static string PeriodText => HelixTexts.Current.ChoosePeriod;

    private ItemsControl? _presets;
    private DateTime? _start;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _presets = GetTemplateChild("PART_Presets") as ItemsControl;
        BuildPresets();
        OnValueChanged();
    }

    private IReadOnlyList<HelixDateRangePreset> EffectivePresets()
    {
        if (Presets is { } presets) return presets;
        var texts = HelixTexts.Current;
        var firstDay = Culture.DateTimeFormat.FirstDayOfWeek;
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
        _presets.Items.Clear();
        foreach (var preset in EffectivePresets())
        {
            var button = new Button { Content = preset.Label, Tag = preset };
            if (TryFindResource("Helix.RangePreset") is Style style) button.Style = style;
            button.Click += (_, _) =>
            {
                SetCurrentValue(SelectedRangeProperty, preset.Range(DateTime.Today));
                Close();
            };
            _presets.Items.Add(button);
        }
    }

    private void OnValueChanged()
    {
        SetDisplay(SelectedRange is { } range
            ? range.Days == 1 ? range.Start.ToString(Format, Culture) : $"{range.Start.ToString(Format, Culture)} – {range.End.ToString(Format, Culture)}"
            : null);
        if (!Syncing) SyncCalendar();
    }

    protected override void OnOpening()
    {
        _start = null;
        SetValue(IsPickingEndKey, false);
    }

    protected override void SyncCalendar()
    {
        if (CalendarPart is null) return;
        Syncing = true;
        CalendarPart.SelectionMode = CalendarSelectionMode.SingleRange;
        CalendarPart.DisplayDateStart = Minimum?.Date;
        CalendarPart.DisplayDateEnd = Maximum?.Date;
        CalendarPart.SelectedDates.Clear();
        if (SelectedRange is { } range)
        {
            CalendarPart.SelectedDates.AddRange(range.Start, range.End);
            CalendarPart.DisplayDate = range.End;
        }

        Syncing = false;
    }

    protected override void OnCalendarPicked()
    {
        if (CalendarPart is null || CalendarPart.SelectedDates.Count == 0) return;
        var clicked = CalendarPart.SelectedDates.Count == 1 ? CalendarPart.SelectedDates[0] : CalendarPart.SelectedDates[^1];

        if (_start is not { } start)
        {
            // First click: the start. Days beyond MaxDays become unreachable.
            _start = clicked.Date;
            SetValue(IsPickingEndKey, true);
            Syncing = true;
            CalendarPart.SelectedDates.Clear();
            CalendarPart.SelectedDates.Add(clicked.Date);
            if (MaxDays is { } max and > 0)
            {
                var from = clicked.Date.AddDays(-(max - 1));
                var to = clicked.Date.AddDays(max - 1);
                CalendarPart.DisplayDateStart = Minimum is { } min && min.Date > from ? min.Date : from;
                CalendarPart.DisplayDateEnd = Maximum is { } mx && mx.Date < to ? mx.Date : to;
            }

            Syncing = false;
            return;
        }

        _start = null;
        SetValue(IsPickingEndKey, false);
        SetCurrentValue(SelectedRangeProperty, new HelixDateRange(start, clicked));
        Close();
    }

    protected override void ClearValue() => SetCurrentValue(SelectedRangeProperty, null);
}
