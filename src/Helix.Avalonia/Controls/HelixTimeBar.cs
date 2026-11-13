using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>A slot chip of <see cref="HelixTimeBar"/>: its times, status text and dot.</summary>
public sealed class HelixTimeBarSlotView
{
    public required HelixTimeBarSlot Slot { get; init; }
    public required string Label { get; init; }
    public string? Text { get; init; }
    public IBrush? Brush { get; init; }
    public bool HasBrush => Brush is not null;
    public bool IsCurrent { get; init; }
    public bool IsPast { get; init; }
}

/// <summary>
/// A live countdown to the next deadline: the time left, a bar of the elapsed period and the slots around now.
/// Repeats every <see cref="Step"/> (end of each slot minus <see cref="Lead"/>) or counts to one fixed <see cref="Deadline"/>.
/// </summary>
[TemplatePart("PART_Track", typeof(Control))]
[TemplatePart("PART_Fill", typeof(Control))]
[PseudoClasses(":warn", ":critical", ":passed")]
public class HelixTimeBar : TemplatedControl
{
    public static readonly StyledProperty<TimeSpan> StepProperty = AvaloniaProperty.Register<HelixTimeBar, TimeSpan>(nameof(Step), TimeSpan.FromMinutes(15));
    public static readonly StyledProperty<TimeSpan> LeadProperty = AvaloniaProperty.Register<HelixTimeBar, TimeSpan>(nameof(Lead));
    public static readonly StyledProperty<TimeSpan> OffsetProperty = AvaloniaProperty.Register<HelixTimeBar, TimeSpan>(nameof(Offset));
    public static readonly StyledProperty<DateTime?> DeadlineProperty = AvaloniaProperty.Register<HelixTimeBar, DateTime?>(nameof(Deadline));
    public static readonly StyledProperty<DateTime?> StartProperty = AvaloniaProperty.Register<HelixTimeBar, DateTime?>(nameof(Start));
    public static readonly StyledProperty<DateTime?> NowProperty = AvaloniaProperty.Register<HelixTimeBar, DateTime?>(nameof(Now));
    public static readonly StyledProperty<bool> RunningProperty = AvaloniaProperty.Register<HelixTimeBar, bool>(nameof(Running), true);
    public static readonly StyledProperty<TimeSpan?> WarningProperty = AvaloniaProperty.Register<HelixTimeBar, TimeSpan?>(nameof(Warning));
    public static readonly StyledProperty<TimeSpan?> CriticalProperty = AvaloniaProperty.Register<HelixTimeBar, TimeSpan?>(nameof(Critical));
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixTimeBar, string?>(nameof(Label));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<HelixTimeBar, string?>(nameof(Hint));
    public static readonly StyledProperty<bool> ShowSlotsProperty = AvaloniaProperty.Register<HelixTimeBar, bool>(nameof(ShowSlots), true);
    public static readonly StyledProperty<int> SlotsBeforeProperty = AvaloniaProperty.Register<HelixTimeBar, int>(nameof(SlotsBefore), 1);
    public static readonly StyledProperty<int> SlotsAfterProperty = AvaloniaProperty.Register<HelixTimeBar, int>(nameof(SlotsAfter), 2);
    public static readonly StyledProperty<Func<HelixTimeBarSlot, string?>?> SlotColorProperty = AvaloniaProperty.Register<HelixTimeBar, Func<HelixTimeBarSlot, string?>?>(nameof(SlotColor));
    public static readonly StyledProperty<Func<HelixTimeBarSlot, string?>?> SlotTextProperty = AvaloniaProperty.Register<HelixTimeBar, Func<HelixTimeBarSlot, string?>?>(nameof(SlotText));
    public static readonly StyledProperty<bool> CompactProperty = AvaloniaProperty.Register<HelixTimeBar, bool>(nameof(Compact));
    public static readonly StyledProperty<string> TimeFormatProperty = AvaloniaProperty.Register<HelixTimeBar, string>(nameof(TimeFormat), "HH:mm");
    public static readonly StyledProperty<double> TrackHeightProperty = AvaloniaProperty.Register<HelixTimeBar, double>(nameof(TrackHeight), 8);
    public static readonly StyledProperty<ICommand?> DeadlineCommandProperty = AvaloniaProperty.Register<HelixTimeBar, ICommand?>(nameof(DeadlineCommand));

    public static readonly DirectProperty<HelixTimeBar, TimeSpan> RemainingProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, TimeSpan>(nameof(Remaining), t => t.Remaining);
    public static readonly DirectProperty<HelixTimeBar, string> RemainingTextProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, string>(nameof(RemainingText), t => t.RemainingText);
    public static readonly DirectProperty<HelixTimeBar, DateTime> UntilProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, DateTime>(nameof(Until), t => t.Until);
    public static readonly DirectProperty<HelixTimeBar, string> UntilTextProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, string>(nameof(UntilText), t => t.UntilText);
    public static readonly DirectProperty<HelixTimeBar, double> ProgressProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, double>(nameof(Progress), t => t.Progress);
    public static readonly DirectProperty<HelixTimeBar, string> StateProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, string>(nameof(State), t => t.State);
    public static readonly DirectProperty<HelixTimeBar, IReadOnlyList<HelixTimeBarSlotView>> SlotsProperty =
        AvaloniaProperty.RegisterDirect<HelixTimeBar, IReadOnlyList<HelixTimeBarSlotView>>(nameof(Slots), t => t.Slots);
    public static readonly DirectProperty<HelixTimeBar, bool> HasSlotsProperty = AvaloniaProperty.RegisterDirect<HelixTimeBar, bool>(nameof(HasSlots), t => t.HasSlots);

    /// <summary>Repeating mode: the length of a slot (15 min).</summary>
    public TimeSpan Step { get => GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>How long before the end of a slot the deadline falls (0: at the end).</summary>
    public TimeSpan Lead { get => GetValue(LeadProperty); set => SetValue(LeadProperty, value); }

    /// <summary>Shift of the slot grid from midnight.</summary>
    public TimeSpan Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>Fixed mode: a single deadline instead of a repeating one.</summary>
    public DateTime? Deadline { get => GetValue(DeadlineProperty); set => SetValue(DeadlineProperty, value); }

    /// <summary>Fixed mode: where the bar starts (the time the control was created by default).</summary>
    public DateTime? Start { get => GetValue(StartProperty); set => SetValue(StartProperty, value); }

    /// <summary>Overrides the current time (tests, replays): the clock runs on from it.</summary>
    public DateTime? Now { get => GetValue(NowProperty); set => SetValue(NowProperty, value); }

    /// <summary>False freezes the clock.</summary>
    public bool Running { get => GetValue(RunningProperty); set => SetValue(RunningProperty, value); }

    /// <summary>Under this time left, the bar turns orange.</summary>
    public TimeSpan? Warning { get => GetValue(WarningProperty); set => SetValue(WarningProperty, value); }

    /// <summary>Under this time left, the bar turns red.</summary>
    public TimeSpan? Critical { get => GetValue(CriticalProperty); set => SetValue(CriticalProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>A line under the bar, e.g. "Activation window: 14:15–14:30".</summary>
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public bool ShowSlots { get => GetValue(ShowSlotsProperty); set => SetValue(ShowSlotsProperty, value); }
    public int SlotsBefore { get => GetValue(SlotsBeforeProperty); set => SetValue(SlotsBeforeProperty, value); }
    public int SlotsAfter { get => GetValue(SlotsAfterProperty); set => SetValue(SlotsAfterProperty, value); }

    /// <summary>A status color per slot: "success", "danger", "#FF8A00"… or null for no dot.</summary>
    public Func<HelixTimeBarSlot, string?>? SlotColor { get => GetValue(SlotColorProperty); set => SetValue(SlotColorProperty, value); }

    /// <summary>A short text per slot ("Sent", "Unavailable"…); the current slot says "Current" by default.</summary>
    public Func<HelixTimeBarSlot, string?>? SlotText { get => GetValue(SlotTextProperty); set => SetValue(SlotTextProperty, value); }

    /// <summary>Label, countdown and bar only.</summary>
    public bool Compact { get => GetValue(CompactProperty); set => SetValue(CompactProperty, value); }
    public string TimeFormat { get => GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }
    public double TrackHeight { get => GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    /// <summary>Executed each time a deadline is reached, with that deadline (DateTime) as parameter.</summary>
    public ICommand? DeadlineCommand { get => GetValue(DeadlineCommandProperty); set => SetValue(DeadlineCommandProperty, value); }

    /// <summary>Raised each time a deadline is reached, with that deadline.</summary>
    public event EventHandler<DateTime>? DeadlineReached;

    /// <summary>A testable clock (system clock by default).</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private TimeSpan _remaining;
    private string _remainingText = "";
    private DateTime _until;
    private string _untilText = "";
    private double _progress;
    private string _stateName = "ok";
    private IReadOnlyList<HelixTimeBarSlotView> _slots = [];
    private bool _hasSlots;

    public TimeSpan Remaining { get => _remaining; private set => SetAndRaise(RemainingProperty, ref _remaining, value); }

    /// <summary>"04:32", or the "deadline passed" text in fixed mode.</summary>
    public string RemainingText { get => _remainingText; private set => SetAndRaise(RemainingTextProperty, ref _remainingText, value); }

    /// <summary>The deadline counted to.</summary>
    public DateTime Until { get => _until; private set => SetAndRaise(UntilProperty, ref _until, value); }

    /// <summary>"until 14:15".</summary>
    public string UntilText { get => _untilText; private set => SetAndRaise(UntilTextProperty, ref _untilText, value); }

    /// <summary>Elapsed share of the period, 0 to 1.</summary>
    public double Progress { get => _progress; private set => SetAndRaise(ProgressProperty, ref _progress, value); }

    /// <summary>"ok", "warn", "critical" or "passed": also the pseudo-classes :warn, :critical, :passed.</summary>
    public string State { get => _stateName; private set => SetAndRaise(StateProperty, ref _stateName, value); }
    public IReadOnlyList<HelixTimeBarSlotView> Slots { get => _slots; private set => SetAndRaise(SlotsProperty, ref _slots, value); }
    public bool HasSlots { get => _hasSlots; private set => SetAndRaise(HasSlotsProperty, ref _hasSlots, value); }

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DateTime _created;
    private DateTime _realAtOverride;
    private DateTime? _frozen;
    private HelixTimeBarState? _state;
    private DateTime _slotKey;
    private Control? _track, _fill;

    public HelixTimeBar()
    {
        _created = DateTime.Now;
        _timer.Tick += (_, _) => Tick();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Update(rebuildSlots: true);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_track is not null) _track.SizeChanged -= OnTrackSized;
        base.OnApplyTemplate(e);
        _track = e.NameScope.Find<Control>("PART_Track");
        _fill = e.NameScope.Find<Control>("PART_Fill");
        if (_track is not null) _track.SizeChanged += OnTrackSized;
        Update(rebuildSlots: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == NowProperty)
        {
            _realAtOverride = RealNow;
            Update(rebuildSlots: true);
        }
        else if (p == RunningProperty)
        {
            if (Running) _frozen = null;
            Update(rebuildSlots: false);
        }
        else if (p == StepProperty || p == LeadProperty || p == OffsetProperty || p == DeadlineProperty || p == StartProperty
                 || p == WarningProperty || p == CriticalProperty || p == ShowSlotsProperty || p == SlotsBeforeProperty
                 || p == SlotsAfterProperty || p == SlotColorProperty || p == SlotTextProperty || p == TimeFormatProperty)
        {
            Update(rebuildSlots: true);
        }
    }

    /// <summary>Recomputes the slot chips (call after your slot statuses changed).</summary>
    public void Refresh() => Update(rebuildSlots: true);

    /// <summary>Advances the clock by one tick now (the timer does it every second); used by tests.</summary>
    public void Tick()
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

    private void OnTrackSized(object? sender, SizeChangedEventArgs e) => LayoutFill();

    private DateTime RealNow => TimeProvider.GetLocalNow().DateTime;

    private DateTime CurrentTime()
    {
        if (Now is { } overridden) return Running ? overridden + (RealNow - _realAtOverride) : overridden;
        return Running ? RealNow : _frozen ??= RealNow;
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
        Remaining = _state.Remaining;
        RemainingText = _state.Passed ? texts.TimeBarPassed : HelixTimeBarMath.FormatRemaining(_state.Remaining);
        Until = _state.Until;
        UntilText = string.Format(culture, texts.TimeBarUntil, _state.Until.ToString(TimeFormat, culture));
        Progress = _state.Progress;
        State = _state.Passed ? "passed"
            : Critical is { } c && _state.Remaining <= c ? "critical"
            : Warning is { } w && _state.Remaining <= w ? "warn" : "ok";
        PseudoClasses.Set(":warn", State == "warn");
        PseudoClasses.Set(":critical", State == "critical");
        PseudoClasses.Set(":passed", State == "passed");

        var showSlots = ShowSlots && Deadline is null;
        HasSlots = showSlots;
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
                    Brush = HelixDraw.Resolve(this, SlotColor?.Invoke(slot)),
                    IsCurrent = i == 0,
                    IsPast = i < 0,
                });
            }

            Slots = views;
        }

        LayoutFill();
    }

    private void LayoutFill()
    {
        if (_track is null || _fill is null || _state is null) return;
        _fill.Width = Math.Max(0, _track.Bounds.Width * _state.Progress);
    }
}
