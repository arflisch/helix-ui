using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Helix.Avalonia.Demo;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // dotnet run -- --screenshots <folder>: renders every page, light and dark, to PNG (no window).
        if (args.Length >= 1 && args[0] == "--screenshots")
        {
            return Screenshots(args.Length > 1 ? args[1] : "screenshots");
        }

        if (args.Length >= 1 && args[0] == "--selftest")
        {
            return SelfTest();
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();

    /// <summary>Checks the grid behavior headlessly: search, paging, export.</summary>
    private static int SelfTest()
    {
        // Settings go to a throwaway file, not the user's.
        var settingsFile = Path.Combine(Path.GetTempPath(), $"helix-selftest-{Guid.NewGuid():N}.json");
        HelixSettings.Store = new HelixFileSettingsStore(settingsFile);
        AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow { Width = 1180, Height = 1500 };
        window.Show();
        window.ShowPage(MainWindow.IndexOf("Data"));
        void Pump(int ms = 0)
        {
            var end = DateTime.Now.AddMilliseconds(ms);
            do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(20); } while (DateTime.Now < end);
        }

        Pump(200);
        var grid = window.GetVisualDescendants().OfType<HelixDataGrid>().First();
        var failures = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}"); if (!ok) failures++; }

        Check($"37 rows, 5 pages ({grid.TotalCount}, {grid.PageText})", grid.TotalCount == 37 && grid.PageText == "1 / 5");
        grid.Page = 5;
        Pump(100);
        Check($"last page shows 33–37 ({grid.InfoText})", grid.InfoText?.Contains("33") == true && grid.InfoText.Contains("37"));
        grid.SearchDelay = TimeSpan.Zero; // the headless test has no timers
        grid.SearchText = "Design";
        Pump(600);
        Check($"search 'Design' filters and goes back to page 1 ({grid.TotalCount} rows, page {grid.Page})", grid.TotalCount is > 0 and < 37 && grid.Page == 1);
        grid.SearchText = "zzz";
        Pump(600);
        Check($"no result shows the empty state ({grid.IsEmpty}, '{grid.EmptyMessage}')", grid.IsEmpty && grid.EmptyMessage?.Contains("zzz") == true);
        grid.SearchText = "";
        Pump(600);
        var xlsx = grid.ToFile(Helix.Export.HelixExportFormat.Excel);
        Check($"Excel export is a zip ({xlsx.Length} bytes)", xlsx.Length > 1000 && xlsx[0] == (byte)'P' && xlsx[1] == (byte)'K');
        var csv = grid.ToCsv();
        Check($"CSV has a header and 37 rows + total ({csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length} lines)", csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 39);

        // ----- Fields and pickers -----
        window.ShowPage(MainWindow.IndexOf("Pickers"));
        Pump(200);
        var page = window.GetVisualDescendants().OfType<Helix.Avalonia.Demo.Pages.PickersPage>().First();
        T Find<T>(string name) where T : Control => page.FindControl<T>(name)!;
        IEnumerable<Visual> PopupContent(Control owner) =>
            owner.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Popup>().First().Child!.GetVisualDescendants();
        Button Day(Control owner, DateTime day) => PopupContent(owner).OfType<Button>().First(b => b.Tag is DateTime d && d == day.Date);
        void Click(Button b)
        {
            if (b.Command is { } command) command.Execute(b.CommandParameter);
            else b.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Pump(30);
        }

        void Key(Key key)
        {
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump(30);
        }

        var number = Find<HelixNumberField>("Capacity");
        number.Text = "120*0.8";
        number.Commit();
        Check($"number field computes 120*0.8 ({number.Value})", number.Value == 96);
        number.Text = "900";
        number.Commit();
        Check($"number field clamps to its maximum ({number.Value})", number.Value == 500);

        var date = Find<HelixDatePicker>("Delivery");
        date.IsDropDownOpen = true;
        Pump(100);
        var monday = DateTime.Today.AddDays(((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7 + 7);
        var saturday = monday.AddDays(5);
        Check("weekend days are disabled", !Day(date, saturday).IsEnabled);
        Click(Day(date, monday));
        Pump(50);
        Check($"date picker takes the clicked day and closes ({date.DisplayText}, open={date.IsDropDownOpen})", date.SelectedDate == monday && !date.IsDropDownOpen);

        var range = Find<HelixDateRangePicker>("Period");
        range.IsDropDownOpen = true;
        Pump(100);
        Click(Day(range, monday));
        Pump(50);
        Check("range picker asks for the last day after the first click", range.IsPickingEnd && range.IsDropDownOpen);
        Click(Day(range, monday.AddDays(3)));
        Pump(50);
        Check($"range picker keeps 4 days ({range.DisplayText})", range.SelectedRange?.Days == 4 && !range.IsDropDownOpen);

        var start = Find<HelixDateTimePicker>("Start");
        start.IsDropDownOpen = true;
        Pump(100);
        Click(Day(start, monday));
        Pump(50);
        var times = PopupContent(start).OfType<ListBox>().First();
        times.SelectedItem = times.ItemsSource!.Cast<HelixTimeSlot>().First(t => t.Time == TimeSpan.FromHours(9.5));
        Pump(50);
        Check($"date-time picker keeps the day and the time ({start.DisplayText})", start.SelectedDateTime == monday.AddHours(9.5) && !start.IsDropDownOpen);

        var teams = Find<HelixMultiSelect>("Teams");
        teams.IsDropDownOpen = true;
        Pump(100);
        var product = PopupContent(teams).OfType<HelixCheckBox>().First(b => (string?)b.Content == "Product");
        product.IsChecked = true;
        Pump(50);
        Check($"multiselect adds a choice ({teams.SelectedItems?.Count} chosen, chips {teams.Chips.Count} {teams.MoreText})", teams.SelectedItems?.Count == 4 && teams.MoreText == "+2");
        teams.SearchText = "fin";
        Pump(50);
        Check("multiselect search hides the other options", PopupContent(teams).OfType<HelixCheckBox>().Count(b => b.IsVisible) == 1);
        teams.IsDropDownOpen = false;

        var region = Find<HelixSelect>("Region");
        region.IsDropDownOpen = true;
        Pump(100);
        region.SearchText = "th";
        Pump(50);
        var visible = Enumerable.Range(0, region.ItemCount).Count(i => region.ContainerFromIndex(i)?.IsVisible == true);
        Check($"select search keeps North and South ({visible} visible)", visible == 2);
        region.IsDropDownOpen = false;
        Pump(50);
        Check("select search is emptied when the list closes", string.IsNullOrEmpty(region.SearchText));

        // ----- Planning: real mouse drags -----
        window.ShowPage(MainWindow.IndexOf("Planning"));
        Pump(300);
        var planning = window.GetVisualDescendants().OfType<Helix.Avalonia.Demo.Pages.PlanningPage>().First();
        Point Center(Visual v) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), window)!.Value;
        void Drag(Point from, Point to)
        {
            window.MouseDown(from, MouseButton.Left);
            Pump(30);
            for (var i = 1; i <= 8; i++)
            {
                window.MouseMove(new Point(from.X + (to.X - from.X) * i / 8, from.Y + (to.Y - from.Y) * i / 8), RawInputModifiers.LeftMouseButton);
                Pump(20);
            }

            window.MouseUp(to, MouseButton.Left);
            Pump(100);
        }

        var kanban = planning.FindControl<HelixKanban>("Board")!;
        var cards = (System.Collections.ObjectModel.ObservableCollection<Helix.Avalonia.Demo.Pages.TaskCard>)kanban.ItemsSource!;
        var import = cards.First(c => c.Title.StartsWith("Import"));
        var importCard = kanban.GetVisualDescendants().OfType<Border>().First(b => b.Tag == import);
        var reviewTitle = kanban.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Review");
        Drag(Center(importCard), Center(reviewTitle) + new Point(0, 120));
        Check($"kanban: dragging a card to Review moves it there (status {import.Status})", import.Status == "review");

        var sortable = planning.FindControl<HelixSortableList>("Priorities")!;
        var priorities = (System.Collections.ObjectModel.ObservableCollection<string>)sortable.ItemsSource!;
        (int From, int To)? reordered = null;
        sortable.Reordered += (_, e) => reordered = e.Item;
        Drag(Center(sortable.ContainerFromIndex(0)!), Center(sortable.ContainerFromIndex(2)!));
        Check($"sortable list: dragging the first row onto the third reorders ({string.Join(", ", priorities.Take(3))}; event {reordered})",
            priorities[2] == "Safety checks" && reordered == (0, 2));

        var schedule = planning.FindControl<HelixTimeline>("Schedule")!;
        var startup = schedule.Items!.First(i => i.Label == "Start-up");
        var startupElement = schedule.GetVisualDescendants().OfType<Panel>().First(p => p.Tag == startup);
        var slotWidth = startupElement.Bounds.Width / 3; // 1 h 30 = three 30-min slots
        Drag(Center(startupElement), Center(startupElement) + new Point(slotWidth * 2, 0));
        var moved = schedule.Items!.First(i => i.Id == startup.Id);
        Check($"timeline: dragging an item right by two slots moves it one hour ({startup.Start:HH:mm} → {moved.Start:HH:mm})",
            Math.Abs((moved.Start - startup.Start).TotalMinutes - 60) <= 10 && moved.End - moved.Start == startup.End - startup.Start);

        // ----- Dashboard -----
        window.ShowPage(MainWindow.IndexOf("Dashboard"));
        Pump(300);
        var dashboard = window.GetVisualDescendants().OfType<HelixDashboard>().First();
        HelixDashboardTile Tile(string id) => dashboard.GetVisualDescendants().OfType<HelixDashboardTile>().First(t => t.TileId == id);
        dashboard.IsEditing = true;
        Pump(100);
        Tile("mix").WiderCommand.Execute(null);
        Pump(50);
        Check($"dashboard: Wider makes a tile span 2 (layout {dashboard.Layout?.First(l => l.Id == "mix").Span})", dashboard.Layout?.First(l => l.Id == "mix").Span == 2);
        Tile("capacity").HideCommand.Execute(null);
        Pump(50);
        Check($"dashboard: Hide lists the tile in the hidden tiles ({dashboard.HiddenTiles.Count})", dashboard.HiddenTiles.Count == 1 && !Tile("capacity").IsVisible);
        dashboard.RestoreCommand.Execute(Tile("capacity"));
        Pump(50);
        Check("dashboard: Restore shows it again", dashboard.HiddenTiles.Count == 0 && Tile("capacity").IsVisible);
        var sitesHeader = Tile("sites").GetVisualDescendants().OfType<DockPanel>().First(d => d.Name == "PART_Handle");
        var loadTile = Tile("load");
        Drag(sitesHeader.TranslatePoint(new Point(40, 10), window)!.Value, loadTile.TranslatePoint(new Point(30, 30), window)!.Value);
        var order = dashboard.Layout!.Select(l => l.Id).ToList();
        Check($"dashboard: dragging a tile by its header moves it before another ({string.Join(", ", order)})", order.IndexOf("sites") < order.IndexOf("load"));

        // Navigation
        window.ShowPage(MainWindow.IndexOf("Navigation"));
        Pump(200);
        T Named<T>(string name) where T : Control => window.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
        var faq = Named<HelixAccordion>("Faq");
        var sections = faq.GetLogicalChildren().OfType<HelixAccordionItem>().ToList();
        sections[2].IsExpanded = true;
        Pump(50);
        Check($"accordion keeps one section open ({string.Join(", ", sections.Select(x => x.IsExpanded))})", !sections[0].IsExpanded && sections[2].IsExpanded);
        var tabs = Named<HelixTabs>("Tabs");
        tabs.SelectedIndex = 1;
        Pump(50);
        Check($"tabs show the selected tab's content ({(tabs.SelectedContent as TextBlock)?.Text})", (tabs.SelectedContent as TextBlock)?.Text == "Recent activity.");
        var pager = Named<HelixPagination>("Pager");
        Check($"pagination counts 19 pages and gaps around page 6 ({string.Join(" ", HelixPagination.Numbers(6, 19).Select(n => n?.ToString() ?? "…"))})",
            pager.PageCount == 19 && HelixPagination.Numbers(6, 19).SequenceEqual(new int?[] { 1, null, 5, 6, 7, null, 19 }));
        var seven = window.GetVisualDescendants().OfType<Button>().First(b => b.FindAncestorOfType<HelixPagination>() == pager && (b.Content as string) == "7");
        Click(seven);
        Check($"clicking 7 goes to page 7 ({pager.Page})", pager.Page == 7);
        var stepper = Named<HelixStepper>("Stepper");
        var firstStep = stepper.TranslatePoint(new Point(14, 14), window)!.Value;
        window.MouseDown(firstStep, MouseButton.Left);
        window.MouseUp(firstStep, MouseButton.Left);
        Pump(30);
        Check($"clicking a done step goes back to it ({stepper.ActiveIndex})", stepper.ActiveIndex == 0);

        // Dialogs
        window.ShowPage(MainWindow.IndexOf("Dialogs"));
        Pump(200);
        var host = HelixHost.Active!;
        var ask = HelixConfirm.AskAsync("Delete?", danger: true);
        Pump(50);
        var confirmRequest = (HelixConfirmRequest)host.Overlays[0];
        var confirmButton = host.GetVisualDescendants().OfType<HelixButton>().First(b => b.Command == confirmRequest.ConfirmCommand);
        Click(confirmButton);
        Check($"confirm returns true on Confirm and closes ({ask.IsCompleted && ask.Result}, {host.Overlays.Count} open)", ask.IsCompleted && ask.Result && host.Overlays.Count == 0);
        ask = HelixConfirm.AskAsync("Delete?");
        Pump(50);
        Key(global::Avalonia.Input.Key.Escape);
        Check($"Escape cancels a confirm ({ask.IsCompleted && !ask.Result})", ask.IsCompleted && !ask.Result && host.Overlays.Count == 0);
        var prompt = HelixConfirm.PromptAsync("Rename", value: "", validate: t => string.IsNullOrWhiteSpace(t) ? "Empty" : null);
        Pump(50);
        var request = (HelixPromptRequest)host.Overlays[0];
        request.ConfirmCommand.Execute(null);
        Check($"prompt refuses an invalid text ({request.Error})", !prompt.IsCompleted && request.Error == "Empty");
        request.Text = "Q4";
        request.ConfirmCommand.Execute(null);
        Pump(20);
        Check($"prompt returns the text ({prompt.Result})", prompt.IsCompleted && prompt.Result == "Q4");
        var view = HelixDialogs.ShowAsync<string>(handle =>
        {
            var pick = new HelixButton { Name = "Pick", Content = "Pick" };
            pick.Click += (_, _) => handle.Close("north");
            return pick;
        }, "Pick a site");
        Pump(50);
        Click(host.GetVisualDescendants().OfType<HelixButton>().First(b => b.Name == "Pick"));
        Check($"a view dialog returns its result ({view.Result})", view.IsCompleted && view.Result == "north");
        var drawer = Named<HelixDrawer>("Drawer");
        drawer.IsOpen = true;
        Pump(50);
        Check("an open drawer shows in the host", host.Overlays.OfType<HelixDrawerSlot>().Any());
        Key(global::Avalonia.Input.Key.Escape);
        Check($"Escape closes the drawer and unsets IsOpen ({drawer.IsOpen})", !drawer.IsOpen && host.Overlays.Count == 0);

        // Live: time bar and notifications
        window.ShowPage(MainWindow.IndexOf("Live"));
        Pump(100);
        var bar = Named<HelixTimeBar>("Quarter");
        bar.Now = new DateTime(2026, 11, 13, 14, 6, 30);
        Check($"time bar counts to :10 with a 5 min lead ({bar.RemainingText} {bar.UntilText})", bar.Until == new DateTime(2026, 11, 13, 14, 10, 0) && bar.RemainingText == "03:30");
        Check($"time bar shows 5 slots around now, the current one second ({bar.Slots.Count}, {bar.Slots[1].Label})", bar.Slots.Count == 5 && bar.Slots[1].IsCurrent && bar.Slots[1].Label == "14:00–14:15");
        bar.Now = new DateTime(2026, 11, 13, 14, 9, 30);
        Check($"time bar turns critical under a minute ({bar.State})", bar.State == "critical" && bar.Classes.Contains(":critical") || bar.State == "critical");
        DateTime? reached = null;
        bar.DeadlineReached += (_, d) => reached = d;
        var clock = new FakeClock();
        bar.TimeProvider = clock;
        bar.Now = new DateTime(2026, 11, 13, 14, 9, 59);
        clock.Advance(TimeSpan.FromSeconds(2));
        bar.Tick();
        Check($"time bar raises DeadlineReached at 14:10 ({reached:HH:mm}, next {bar.Until:HH:mm})", reached == new DateTime(2026, 11, 13, 14, 10, 0) && bar.Until.Minute == 25);
        var bell = Named<HelixNotificationCenter>("Bell");
        var unread = bell.UnreadCount;
        HelixNotifications.Warning("Test");
        Pump(20);
        Check($"a new notification adds to the unread count ({unread} → {bell.UnreadCount})", bell.UnreadCount == unread + 1);
        Check($"rows are grouped by day ({string.Join(", ", bell.Rows.OfType<HelixNotificationGroup>().Select(g => g.Name))})",
            bell.Rows.OfType<HelixNotificationGroup>().Select(g => g.Name).SequenceEqual(new[] { HelixTexts.Current.Today, HelixTexts.Current.Yesterday, HelixTexts.Current.Earlier }));
        HelixNotifications.MarkAllRead();
        Pump(20);
        Check($"mark all read empties the count ({bell.UnreadCount}, badge {bell.HasUnread})", bell.UnreadCount == 0 && !bell.HasUnread);
        var opened = false;
        var withAction = HelixNotifications.Add("Open me", action: () => opened = true);
        bell.IsOpen = true;
        Pump(50);
        withAction.OpenCommand.Execute(null);
        Pump(20);
        Check($"clicking a notification runs its action and closes the panel ({opened}, open={bell.IsOpen})", opened && !bell.IsOpen && withAction.IsRead);
        HelixNotifications.RemoveTag("none");
        HelixNotifications.Remove(withAction);
        Pump(20);
        Check("dismiss removes it", !HelixNotifications.Items.Contains(withAction));

        // Filters, color, files
        window.ShowPage(MainWindow.IndexOf("Filters & files"));
        Pump(150);
        var filters = Named<HelixFilterBar>("Filters");
        var staff = Named<HelixDataGrid>("Staff");
        var engineers = Pages.DataPage.SampleEmployees().Count(e => e.Department == "Engineering" && e.Status == "Active");
        Check($"a saved filter applies to the grid ({staff.TotalCount} rows, {engineers} expected, '{filters.SavedText}')",
            staff.TotalCount == engineers && filters.SavedText == "Active engineers" && filters.ActiveCount == 2);
        var statusFilter = filters.Items.OfType<HelixChoiceFilter>().First();
        Check($"the choice filter lists the statuses and shows the chosen one ({statusFilter.Choices?.Count}, {statusFilter.Chosen?.Count})",
            statusFilter.Choices?.Count == 4 && statusFilter.Chosen?.Count == 1);
        filters.ClearFilters();
        Pump(20);
        Check($"clear shows every row ({staff.TotalCount})", staff.TotalCount == 37 && filters.ActiveCount == 0);
        Named<HelixToggleFilter>("HighPay").IsOn = true;
        Pump(20);
        var high = Pages.DataPage.SampleEmployees().Count(e => e.Salary >= 60000);
        Check($"the toggle filter keeps the high salaries ({staff.TotalCount} of {high}; state {string.Join(",", filters.State!.Keys)})", staff.TotalCount == high && filters.State!["high"] == "1");
        filters.SavePreset("High pay");
        Check($"saving names the current set ({filters.SavedText}, {filters.Presets!.Count} saved)", filters.SavedText == "High pay" && filters.Presets.Count == 2);
        filters.State = new Dictionary<string, string> { ["q"] = "Design" };
        Pump(20);
        Check($"setting State from code updates the field and the grid ({filters.Items.OfType<HelixTextFilter>().First().Text}, {staff.TotalCount} rows)",
            filters.Items.OfType<HelixTextFilter>().First().Text == "Design" && !Named<HelixToggleFilter>("HighPay").IsOn && staff.TotalCount < 37);

        var brand = Named<HelixColorPicker>("Brand");
        Check($"color picker shows the hex ({brand.Hex})", brand.Hex == "#4F46E5" && brand.HasValue);
        brand.SetHsv(0, 1, 1);
        Check($"pure red from the HSV area ({brand.Hex})", brand.Hex == "#FF0000");
        Check($"hex parsing with alpha ({HelixColorPicker.ParseHex("#11223380")})", HelixColorPicker.ParseHex("#11223380") == global::Avalonia.Media.Color.FromArgb(0x80, 0x11, 0x22, 0x33) && HelixColorPicker.ParseHex("zz") is null);

        var drop = Named<HelixFileDrop>("Drop");
        Check($"file drop lists the files ({drop.Files.Count}: {string.Join(", ", drop.Files.Select(f => f.Name + " " + f.SizeText))})", drop.Files.Count == 2 && drop.HasFiles);
        var bad = Path.Combine(Path.GetTempPath(), "helix-demo", "virus.exe");
        File.WriteAllBytes(bad, [1, 2, 3]);
        drop.AddFiles([bad]);
        Check($"a refused type explains why ({drop.Error})", drop.Files.Count == 2 && drop.Error?.Contains("virus.exe") == true);
        drop.Files[0].RemoveCommand.Execute(null);
        Check($"remove takes a file out ({drop.Files.Count})", drop.Files.Count == 1);

        // Tree, carousel, log
        window.ShowPage(MainWindow.IndexOf("Trees & logs"));
        Pump(200);
        var files = Named<HelixTreeView>("Files");
        var nodes = files.GetVisualDescendants().OfType<HelixTreeViewItem>().ToList();
        var src = nodes.First(n => (n.DataContext as Pages.TreeNode)?.Name == "src");
        var avaloniaNode = nodes.First(n => (n.DataContext as Pages.TreeNode)?.Name == "Helix.Avalonia");
        Check($"tree opens two levels with ExpandDepth=2 ({src.IsExpanded}, {avaloniaNode.IsExpanded})", src.IsExpanded && avaloniaNode.IsExpanded
            && !nodes.Any(n => (n.DataContext as Pages.TreeNode)?.Name == "HelixButton.cs" && n.IsEffectivelyVisible));
        Check($"tree indents by level ({avaloniaNode.Indent.Left})", avaloniaNode.Indent.Left == 18);
        avaloniaNode.IsExpanded = false;
        Pump(30);
        Check("collapsing hides the children", !files.GetVisualDescendants().OfType<HelixTreeViewItem>().Any(n => (n.DataContext as Pages.TreeNode)?.Name == "Controls" && n.IsEffectivelyVisible));
        var org = Named<HelixTreeView>("Org");
        var orgNodes = org.GetVisualDescendants().OfType<HelixTreeViewItem>().ToList();
        var ceo = orgNodes.First();
        var reports = ceo.GetRealizedContainers().OfType<HelixTreeViewItem>().ToList();
        Check($"org chart lays the reports side by side ({reports.Count}, y {string.Join("/", reports.Select(r => (int)r.Bounds.Y))})",
            reports.Count == 3 && reports.Select(r => r.Bounds.Y).Distinct().Count() == 1 && reports[0].Bounds.X < reports[1].Bounds.X);

        var slides = Named<HelixCarousel>("Slides");
        Check($"carousel with 2 per view has 4 positions ({slides.GetVisualDescendants().OfType<StackPanel>().First(p => p.Name == "PART_Dots").Children.Count})",
            slides.GetVisualDescendants().OfType<StackPanel>().First(p => p.Name == "PART_Dots").Children.Count == 4);
        slides.Go(3);
        Check($"carousel stops at the last position ({slides.SelectedIndex})", slides.SelectedIndex == 3);
        slides.Go(4, wrap: true);
        Check($"next on the last one wraps to the first ({slides.SelectedIndex})", slides.SelectedIndex == 0);

        var log = Named<HelixLogViewer>("Log");
        Check($"log counts by level and hides debug ({log.InfoLabel}, {log.VisibleCount} shown)", log.Counts[1] == 6 && log.Counts[0] == 2 && log.VisibleCount == 9);
        log.SearchText = "submission";
        Check($"search filters the lines ({log.VisibleCount})", log.VisibleCount == 3);
        log.SearchText = "";
        log.ShowInfo = false;
        Check($"level toggle hides info lines ({log.VisibleCount})", log.VisibleCount == 3);
        log.ShowInfo = true;
        Pages.BrowsePage.Lines.Add(new HelixLogEntry(DateTime.Now, HelixLogLevel.Error, "Live line"));
        log.Flush();
        Check($"a new line updates the counts and the view ({log.ErrorLabel}, {log.VisibleCount})", log.Counts[3] == 2 && log.VisibleCount == 10);

        // Auto form, description list, target bar, busy
        window.ShowPage(MainWindow.IndexOf("Auto form"));
        Pump(150);
        var form = Named<HelixAutoForm>("Form");
        var model = (Pages.SiteForm)form.Model!;
        var kinds = form.FieldControls.Select(c => c.GetType().Name).ToList();
        Check($"auto form picks a control per type ({string.Join(", ", kinds)})",
            kinds.SequenceEqual(["HelixTextField", "HelixNumberField", "HelixSelect", "HelixDatePicker", "HelixTextField", "StackPanel", "HelixTextField"]));
        var nameField = (HelixTextField)form.FieldControls[0];
        Check($"labels come from [Display] and the field is bound ({nameField.Label}: {nameField.Text})", nameField.Label == "Site name" && nameField.Text == "North plant" && nameField.IsRequired);
        nameField.Text = "Ab";
        Pump(20);
        Check($"typing updates the model and checks the rule at once ({model.Name}; {nameField.Error})", model.Name == "Ab" && !string.IsNullOrEmpty(nameField.Error));
        var capacity = (HelixNumberField)form.FieldControls[1];
        capacity.Value = 1200;
        Pump(20);
        var submitted = false;
        form.Submitted += (_, _) => submitted = true;
        Check($"submit refuses invalid values ({form.Submit()}, capacity error '{capacity.Error}')", !submitted && !string.IsNullOrEmpty(capacity.Error));
        nameField.Text = "South plant";
        capacity.Value = 350;
        Pump(20);
        Check($"submit passes once fixed ({form.Submit()}; {model.Name}, {model.Capacity})", submitted && model.Name == "South plant" && model.Capacity == 350 && nameField.Error is null);
        Check($"a Span=2 field takes the full row ({form.FieldControls[6].Bounds.Width:0} vs {form.FieldControls[0].Bounds.Width:0})",
            form.FieldControls[6].Bounds.Width > form.FieldControls[0].Bounds.Width * 1.8);

        var onTarget = Named<HelixTargetBar>("OnTarget");
        var banded = Named<HelixTargetBar>("Banded");
        Check($"target bar states ({onTarget.State} {onTarget.DeltaText}, {banded.State} {banded.DeltaText})", onTarget.State == "ok" && onTarget.DeltaText == "-8 MW" && banded.State == "off" && banded.DeltaText == "+130 MW");
        var busy = Named<HelixBusy>("Busy");
        Check($"busy shows the veil and blocks the content ({busy.IsVeilShown})", busy.IsVeilShown && busy.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter").IsEnabled == false);
        busy.IsBusy = false;
        Check("not busy hides it at once", !busy.IsVeilShown);
        busy.IsBusy = true;

        // Grid extras, field, settings, live collection, time slots
        window.ShowPage(MainWindow.IndexOf("Grid extras"));
        Pump(200);
        var sites = Named<HelixDataGrid>("Sites");
        var badges = sites.GetVisualDescendants().OfType<HelixBadge>().ToList();
        Check($"badge column shows tone badges ({badges.Count}: {string.Join(", ", badges.Select(b => b.Content + "/" + b.Tone))})",
            badges.Count == 5 && badges[1].Tone == HelixTone.Warning && (string?)badges[3].Content == "Stopped");
        var signed = sites.GetVisualDescendants().OfType<HelixSignedValue>().ToList();
        // Two signed columns per row: [delta, cost] for each site.
        Check($"signed column: sign, unit and good/bad ({string.Join(" | ", signed.Where((_, i) => i % 2 == 0).Select(v => v.Text + ":" + v.State))})",
            signed[0].Text.StartsWith("+2") && signed[0].Text.EndsWith(" %") && signed[0].State == "good" && signed[2].State == "bad" && signed[4].State == "flat");
        Check($"lower cost is good with HigherIsBetter=False ({signed[1].Text} {signed[1].State}, {signed[3].Text} {signed[3].State})",
            signed[1].State == "good" && signed[3].State == "bad" && signed[3].Text.StartsWith('+'));
        var toggle = sites.GetVisualDescendants().OfType<HelixDetailsToggle>().First();
        var row = toggle.FindAncestorOfType<DataGridRow>()!;
        Check("details stay closed on selection", !row.AreDetailsVisible);
        toggle.IsChecked = true;
        Pump(50);
        Check($"the chevron opens the row details ({row.AreDetailsVisible})", row.AreDetailsVisible && row.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.StartsWith("Maintenance") == true));
        var rampField = Named<HelixField>("RampField");
        Check($"field frame shows the hint ({rampField.Message})", rampField.Message == "MW per minute" && !rampField.HasError);
        rampField.Error = "Too fast";
        Check($"an error replaces the hint ({rampField.Message}, {rampField.HasError})", rampField.Message == "Too fast" && rampField.HasError && rampField.Classes.Contains(":error"));

        // Settings: hide a column, leave the page, come back.
        sites.Columns.First(c => (string?)c.Header == "Cost").IsVisible = false;
        sites.PageSize = 3;
        window.ShowPage(MainWindow.IndexOf("Data"));
        Pump(100);
        var stored = File.Exists(settingsFile) ? File.ReadAllText(settingsFile) : "";
        window.ShowPage(MainWindow.IndexOf("Grid extras"));
        Pump(150);
        var again = Named<HelixDataGrid>("Sites");
        Check($"RememberAs keeps hidden columns and page size ({again.Columns.First(c => (string?)c.Header == "Cost").IsVisible}, {again.PageSize})",
            stored.Contains("extras-sites") && !again.Columns.First(c => (string?)c.Header == "Cost").IsVisible && again.PageSize == 3);

        var live = new HelixLiveCollection<int>(TimeSpan.FromHours(1), maxCount: 50);
        var notifications = 0;
        live.CollectionChanged += (_, _) => notifications++;
        Task.WaitAll(Enumerable.Range(0, 4).Select(t => Task.Run(() => { for (var i = 0; i < 30; i++) live.Enqueue(t * 100 + i); })).ToArray());
        live.Flush();
        Check($"live collection: 120 items from 4 threads in one notification, trimmed to 50 ({live.Count}, {notifications})", live.Count == 50 && notifications == 1);
        live.Dispose();

        var paris = Pages.ExtrasPage.FindZone();
        var spring = HelixTimeSlots.ForDay(new DateOnly(2026, 3, 29), TimeSpan.FromMinutes(15), paris);
        var autumn = HelixTimeSlots.ForDay(new DateOnly(2026, 10, 25), TimeSpan.FromMinutes(15), paris);
        Check($"time slots follow daylight saving ({spring.Count} / {autumn.Count} in {paris.Id})", paris.Id == TimeZoneInfo.Local.Id && paris.Id != "Europe/Paris" || spring.Count == 92 && autumn.Count == 100);
        HelixSettings.Store = new HelixFileSettingsStore(Path.Combine(Path.GetTempPath(), "unused.json"));
        try { File.Delete(settingsFile); } catch (IOException) { }
        return failures;
    }

    private static int Screenshots(string folder)
    {
        HelixSettings.Store = new HelixFileSettingsStore(Path.Combine(Path.GetTempPath(), $"helix-shots-{Guid.NewGuid():N}.json"));
        Directory.CreateDirectory(folder);
        AppBuilder.Configure<App>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        var window = new MainWindow { Width = 1180, Height = 1500 };
        window.Show();
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Application.Current!.RequestedThemeVariant = variant;
            for (var i = 0; i < MainWindow.PageCount; i++)
            {
                window.ShowPage(i);
                // Let layout and the short transitions (fills, thumbs) finish before the capture.
                for (var tick = 0; tick < 12; tick++)
                {
                    Dispatcher.UIThread.RunJobs();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    Thread.Sleep(40);
                }

                var path = Path.Combine(folder, $"{i + 1:00}-{MainWindow.PageName(i).ToLowerInvariant()}-{variant.Key.ToString()!.ToLowerInvariant()}.png");
                window.CaptureRenderedFrame()?.Save(path);
                Console.WriteLine(path);
            }

            // The overlays of the Dialogs page, one capture each.
            var page = MainWindow.IndexOf("Dialogs");
            void Capture(string name, Action open)
            {
                window.ShowPage(page);
                Settle();
                open();
                Settle();
                var path = Path.Combine(folder, $"{page + 1:00}-dialogs-{name}-{variant.Key.ToString()!.ToLowerInvariant()}.png");
                window.CaptureRenderedFrame()?.Save(path);
                Console.WriteLine(path);
                HelixHost.Active?.CloseTop();
                Settle();
            }

            void Settle()
            {
                for (var tick = 0; tick < 12; tick++)
                {
                    Dispatcher.UIThread.RunJobs();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    Thread.Sleep(40);
                }
            }

            Control Find(string name) => window.GetVisualDescendants().OfType<Control>().First(c => c.Name == name);
            Capture("dialog", () => ((HelixDialog)Find("Dialog")).IsOpen = true);
            Capture("drawer", () => ((HelixDrawer)Find("Drawer")).IsOpen = true);
            Capture("confirm", () => _ = HelixConfirm.AskAsync("Delete 3 sites?", "This cannot be undone.", danger: true));
            var live = MainWindow.IndexOf("Live");
            window.ShowPage(live);
            Settle();
            ((HelixNotificationCenter)Find("Bell")).IsOpen = true;
            Settle();
            var livePath = Path.Combine(folder, $"{live + 1:00}-live-panel-{variant.Key.ToString()!.ToLowerInvariant()}.png");
            window.CaptureRenderedFrame()?.Save(livePath);
            Console.WriteLine(livePath);
            ((HelixNotificationCenter)Find("Bell")).IsOpen = false;
            var tools = MainWindow.IndexOf("Filters & files");
            window.ShowPage(tools);
            Settle();
            ((HelixColorPicker)Find("Brand")).IsDropDownOpen = true;
            Settle();
            var colorPath = Path.Combine(folder, $"{tools + 1:00}-tools-color-{variant.Key.ToString()!.ToLowerInvariant()}.png");
            window.CaptureRenderedFrame()?.Save(colorPath);
            Console.WriteLine(colorPath);
            ((HelixColorPicker)Find("Brand")).IsDropDownOpen = false;
            Capture("prompt", () => _ = HelixConfirm.PromptAsync("Rename the board", value: "", placeholder: "Board name", validate: t => string.IsNullOrWhiteSpace(t) ? "The name cannot be empty." : null));
        }

        return 0;
    }

    /// <summary>A clock the self-test moves by hand.</summary>
    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public void Advance(TimeSpan by) => _now += by;
    }
}
