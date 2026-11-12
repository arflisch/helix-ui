using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>A slot chip of <see cref="HelixTimeBar"/>: its times, status text and dot.</summary>
public sealed class HelixTimeBarSlotView
{
    public required HelixTimeBarSlot Slot { get; init; }
    public required string Label { get; init; }
    public string? Text { get; init; }
    public Brush? Brush { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsPast { get; init; }
}

/// <summary>
/// A live countdown to the next deadline: the time left, a bar of the elapsed period and the slots around now.
/// Repeats every <see cref="Step"/> (end of each slot minus <see cref="Lead"/>) or counts to one fixed <see cref="Deadline"/>.
/// </summary>
[TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_Fill", Type = typeof(FrameworkElement))]
public class HelixTimeBar : Control
{
    static HelixTimeBar() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixTimeBar), new FrameworkPropertyMetadata(typeof(HelixTimeBar)));

    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixTimeBar)d).Update(rebuildSlots: true);

    public static readonly DependencyProperty StepProperty = HelixProperty.Register<HelixTimeBar, TimeSpan>(nameof(Step), TimeSpan.FromMinutes(15), Changed);
    public static readonly DependencyProperty LeadProperty = HelixProperty.Register<HelixTimeBar, TimeSpan>(nameof(Lead), TimeSpan.Zero, Changed);
    public static readonly DependencyProperty OffsetProperty = HelixProperty.Register<HelixTimeBar, TimeSpan>(nameof(Offset), TimeSpan.Zero, Changed);
    public static readonly DependencyProperty DeadlineProperty = HelixProperty.Register<HelixTimeBar, DateTime?>(nameof(Deadline), null, Changed);
    public static readonly DependencyProperty StartProperty = HelixProperty.Register<HelixTimeBar, DateTime?>(nameof(Start), null, Changed);
    public static readonly DependencyProperty NowProperty = HelixProperty.Register<HelixTimeBar, DateTime?>(nameof(Now), null, (d, _) => ((HelixTimeBar)d).OnNowChanged());
    public static readonly DependencyProperty RunningProperty = HelixProperty.Register<HelixTimeBar, bool>(nameof(Running), true, (d, _) => ((HelixTimeBar)d).OnRunningChanged());
    public static readonly DependencyProperty WarningProperty = HelixProperty.Register<HelixTimeBar, TimeSpan?>(nameof(Warning), null, Changed);
    public static readonly DependencyProperty CriticalProperty = HelixProperty.Register<HelixTimeBar, TimeSpan?>(nameof(Critical), null, Changed);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixTimeBar, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixTimeBar, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ShowSlotsProperty = HelixProperty.Register<HelixTimeBar, bool>(nameof(ShowSlots), true, Changed);
    public static readonly DependencyProperty SlotsBeforeProperty = HelixProperty.Register<HelixTimeBar, int>(nameof(SlotsBefore), 1, Changed);
    public static readonly DependencyProperty SlotsAfterProperty = HelixProperty.Register<HelixTimeBar, int>(nameof(SlotsAfter), 2, Changed);
    public static readonly DependencyProperty SlotColorProperty = HelixProperty.Register<HelixTimeBar, Func<HelixTimeBarSlot, string?>?>(nameof(SlotColor), null, Changed);
    public static readonly DependencyProperty SlotTextProperty = HelixProperty.Register<HelixTimeBar, Func<HelixTimeBarSlot, string?>?>(nameof(SlotText), null, Changed);
    public static readonly DependencyProperty CompactProperty = HelixProperty.Register<HelixTimeBar, bool>(nameof(Compact), false);
    public static readonly DependencyProperty TimeFormatProperty = HelixProperty.Register<HelixTimeBar, string>(nameof(TimeFormat), "HH:mm", Changed);
    public static readonly DependencyProperty TrackHeightProperty = HelixProperty.Register<HelixTimeBar, double>(nameof(TrackHeight), 8d);
    public static readonly DependencyProperty DeadlineCommandProperty = HelixProperty.Register<HelixTimeBar, ICommand?>(nameof(DeadlineCommand), null);

    private static readonly DependencyPropertyKey RemainingKey = DependencyProperty.RegisterReadOnly(nameof(Remaining), typeof(TimeSpan), typeof(HelixTimeBar), new PropertyMetadata(TimeSpan.Zero));
    private static readonly DependencyPropertyKey RemainingTextKey = DependencyProperty.RegisterReadOnly(nameof(RemainingText), typeof(string), typeof(HelixTimeBar), new PropertyMetadata(""));
    private static readonly DependencyPropertyKey UntilKey = DependencyProperty.RegisterReadOnly(nameof(Until), typeof(DateTime), typeof(HelixTimeBar), new PropertyMetadata(DateTime.MinValue));
    private static readonly DependencyPropertyKey UntilTextKey = DependencyProperty.RegisterReadOnly(nameof(UntilText), typeof(string), typeof(HelixTimeBar), new PropertyMetadata(""));
    private static readonly DependencyPropertyKey ProgressKey = DependencyProperty.RegisterReadOnly(nameof(Progress), typeof(double), typeof(HelixTimeBar), new PropertyMetadata(0d));
    private static readonly DependencyPropertyKey StateKey = DependencyProperty.RegisterReadOnly(nameof(State), typeof(string), typeof(HelixTimeBar), new PropertyMetadata("ok"));
    private static readonly DependencyPropertyKey SlotsKey = DependencyProperty.RegisterReadOnly(nameof(Slots), typeof(IReadOnlyList<HelixTimeBarSlotView>), typeof(HelixTimeBar), new PropertyMetadata(Array.Empty<HelixTimeBarSlotView>()));
    private static readonly DependencyPropertyKey HasSlotsKey = DependencyProperty.RegisterReadOnly(nameof(HasSlots), typeof(bool), typeof(HelixTimeBar), new PropertyMetadata(false));
    public static readonly DependencyProperty RemainingProperty = RemainingKey.DependencyProperty;
    public static readonly DependencyProperty RemainingTextProperty = RemainingTextKey.DependencyProperty;
    public static readonly DependencyProperty UntilProperty = UntilKey.DependencyProperty;
    public static readonly DependencyProperty UntilTextProperty = UntilTextKey.DependencyProperty;
    public static readonly DependencyProperty ProgressProperty = ProgressKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;
    public static readonly DependencyProperty SlotsProperty = SlotsKey.DependencyProperty;
    public static readonly DependencyProperty HasSlotsProperty = HasSlotsKey.DependencyProperty;

    /// <summary>Repeating mode: the length of a slot (15 min).</summary>
    public TimeSpan Step { get => (TimeSpan)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>How long before the end of a slot the deadline falls (0: at the end).</summary>
    public TimeSpan Lead { get => (TimeSpan)GetValue(LeadProperty); set => SetValue(LeadProperty, value); }

    /// <summary>Shift of the slot grid from midnight.</summary>
    public TimeSpan Offset { get => (TimeSpan)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>Fixed mode: a single deadline instead of a repeating one.</summary>
    public DateTime? Deadline { get => (DateTime?)GetValue(DeadlineProperty); set => SetValue(DeadlineProperty, value); }

    /// <summary>Fixed mode: where the bar starts (the time the control was created by default).</summary>
    public DateTime? Start { get => (DateTime?)GetValue(StartProperty); set => SetValue(StartProperty, value); }

    /// <summary>Overrides the current time (tests, replays): the clock runs on from it.</summary>
    public DateTime? Now { get => (DateTime?)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    /// <summary>False freezes the clock.</summary>
    public bool Running { get => (bool)GetValue(RunningProperty); set => SetValue(RunningProperty, value); }

    /// <summary>Under this time left, the bar turns orange.</summary>
    public TimeSpan? Warning { get => (TimeSpan?)GetValue(WarningProperty); set => SetValue(WarningProperty, value); }

    /// <summary>Under this time left, the bar turns red.</summary>
    public TimeSpan? Critical { get => (TimeSpan?)GetValue(CriticalProperty); set => SetValue(CriticalProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>A line under the bar, e.g. "Activation window: 14:15–14:30".</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public bool ShowSlots { get => (bool)GetValue(ShowSlotsProperty); set => SetValue(ShowSlotsProperty, value); }
    public int SlotsBefore { get => (int)GetValue(SlotsBeforeProperty); set => SetValue(SlotsBeforeProperty, value); }
    public int SlotsAfter { get => (int)GetValue(SlotsAfterProperty); set => SetValue(SlotsAfterProperty, value); }

    /// <summary>A status color per slot: "success", "danger", "#FF8A00"… or null for no dot.</summary>
    public Func<HelixTimeBarSlot, string?>? SlotColor { get => (Func<HelixTimeBarSlot, string?>?)GetValue(SlotColorProperty); set => SetValue(SlotColorProperty, value); }

    /// <summary>A short text per slot ("Sent", "Unavailable"…); the current slot says "Current" by default.</summary>
    public Func<HelixTimeBarSlot, string?>? SlotText { get => (Func<HelixTimeBarSlot, string?>?)GetValue(SlotTextProperty); set => SetValue(SlotTextProperty, value); }

    /// <summary>Label, countdown and bar only.</summary>
    public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }
    public string TimeFormat { get => (string)GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }
    public double TrackHeight { get => (double)GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    /// <summary>Executed each time a deadline is reached, with that deadline (DateTime) as parameter.</summary>
    public ICommand? DeadlineCommand { get => (ICommand?)GetValue(DeadlineCommandProperty); set => SetValue(DeadlineCommandProperty, value); }

    /// <summary>Raised each time a deadline is reached, with that deadline.</summary>
    public event EventHandler<DateTime>? DeadlineReached;

    /// <summary>A testable clock (system clock by default).</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    public TimeSpan Remaining => (TimeSpan)GetValue(RemainingProperty);

    /// <summary>"04:32", or the "deadline passed" text in fixed mode.</summary>
    public string RemainingText => (string)GetValue(RemainingTextProperty);

    /// <summary>The deadline counted to.</summary>
    public DateTime Until => (DateTime)GetValue(UntilProperty);

    /// <summary>"until 14:15".</summary>
    public string UntilText => (string)GetValue(UntilTextProperty);

    /// <summary>Elapsed share of the period, 0 to 1.</summary>
    public double Progress => (double)GetValue(ProgressProperty);

    /// <summary>"ok", "warn", "critical" or "passed": drives the colors.</summary>
    public string State => (string)GetValue(StateProperty);
    public IReadOnlyList<HelixTimeBarSlotView> Slots => (IReadOnlyList<HelixTimeBarSlotView>)GetValue(SlotsProperty);
    public bool HasSlots => (bool)GetValue(HasSlotsProperty);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DateTime _created;
    private DateTime _realAtOverride;
    private DateTime? _frozen;
    private HelixTimeBarState? _state;
    private DateTime _slotKey;
    private FrameworkElement? _track, _fill;

    public HelixTimeBar()
    {
        _created = DateTime.Now;
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) => { Update(rebuildSlots: true); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    public override void OnApplyTemplate()
    {
        if (_track is not null) _track.SizeChanged -= OnTrackSized;
        base.OnApplyTemplate();
        _track = GetTemplateChild("PART_Track") as FrameworkElement;
        _fill = GetTemplateChild("PART_Fill") as FrameworkElement;
        if (_track is not null) _track.SizeChanged += OnTrackSized;
        Update(rebuildSlots: true);
    }

    /// <summary>Recomputes the slot chips (call after your slot statuses changed).</summary>
    public void Refresh() => Update(rebuildSlots: true);

    private void OnTrackSized(object sender, SizeChangedEventArgs e) => LayoutFill();

    private DateTime RealNow => TimeProvider.GetLocalNow().DateTime;

    private DateTime CurrentTime()
    {
        if (Now is { } overridden)
        {
            return Running ? overridden + (RealNow - _realAtOverride) : overridden;
        }

        return Running ? RealNow : _frozen ??= RealNow;
    }

    private void OnNowChanged()
    {
        _realAtOverride = RealNow;
        Update(rebuildSlots: true);
    }

    private void OnRunningChanged()
    {
        if (Running) _frozen = null;
        Update(rebuildSlots: false);
    }

    private void Tick()
    {
        if (!Running) return;
        var previous = _state;
        Update(rebuildSlots: false);
        if (previous is null || _state is null) return;
        var reached = _state.Until != previous.Until && previous.Until <= _state.Now ? previous.Until
            : Deadline is not null && _state.Passed && !previous.Passed ? _state.Until
            : (DateTime?)null;
        if (reached is { } deadline)
        {
            DeadlineReached?.Invoke(this, deadline);
            if (DeadlineCommand?.CanExecute(deadline) == true) DeadlineCommand.Execute(deadline);
        }
    }

    private void Update(bool rebuildSlots)
    {
        var now = CurrentTime();
        var step = Step > TimeSpan.Zero ? Step : TimeSpan.FromMinutes(15);
        _state = Deadline is { } deadline
            ? HelixTimeBarMath.Fixed(now, Start ?? Now ?? _created, deadline)
            : HelixTimeBarMath.Repeating(now, step, Lead, Offset);

        var culture = CultureInfo.CurrentCulture;
        var texts = HelixTexts.Current;
        SetValue(RemainingKey, _state.Remaining);
        SetValue(RemainingTextKey, _state.Passed ? texts.TimeBarPassed : HelixTimeBarMath.FormatRemaining(_state.Remaining));
        SetValue(UntilKey, _state.Until);
        SetValue(UntilTextKey, string.Format(culture, texts.TimeBarUntil, _state.Until.ToString(TimeFormat, culture)));
        SetValue(ProgressKey, _state.Progress);
        SetValue(StateKey, _state.Passed ? "passed"
            : Critical is { } c && _state.Remaining <= c ? "critical"
            : Warning is { } w && _state.Remaining <= w ? "warn" : "ok");

        var showSlots = ShowSlots && Deadline is null;
        SetValue(HasSlotsKey, showSlots);
        var current = HelixTimeBarMath.SlotStart(now, step, Offset);
        if (showSlots && (rebuildSlots || current != _slotKey))
        {
            _slotKey = current;
            var views = new List<HelixTimeBarSlotView>();
            for (var i = -Math.Max(0, SlotsBefore); i <= Math.Max(0, SlotsAfter); i++)
            {
                var start = current + step * i;
                var slot = new HelixTimeBarSlot(start, start + step, i);
                views.Add(new HelixTimeBarSlotView
                {
                    Slot = slot,
                    Label = $"{start.ToString(TimeFormat, culture)}–{(start + step).ToString(TimeFormat, culture)}",
                    Text = SlotText?.Invoke(slot) ?? (i == 0 ? texts.TimeBarCurrent : null),
                    Brush = HelixTheme.Resolve(this, SlotColor?.Invoke(slot)),
                    IsCurrent = i == 0,
                    IsPast = i < 0,
                });
            }

            SetValue(SlotsKey, views);
        }

        LayoutFill();
    }

    private void LayoutFill()
    {
        if (_track is null || _fill is null || _state is null) return;
        _fill.Width = Math.Max(0, _track.ActualWidth * _state.Progress);
    }
}
