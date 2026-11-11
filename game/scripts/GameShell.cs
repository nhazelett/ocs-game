using Godot;
using System.Text.Json;

namespace LinesOfSupport.Game;

public partial class GameShell : Control
{
    private readonly GameSessionBridge session = new();
    private IReadOnlyList<ScenarioFolder> scenarios = [];
    private string[] replays = [];
    internal string? ScenarioRootOverride { get; set; }
    internal GameSessionBridge Session => session;
    private SchematicMap Map => GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");

    public override void _Ready()
    {
        LoadLook();
        BuildReadableChrome();
        GetNode<Label>("%GameTitle").Text = ProjectSettings.GetSetting("application/config/name").AsString();
        GetNode<Button>("%NextDay").Pressed += OnNextDay;
        GetNode<Button>("%ReplayButton").Pressed += OnReplay;
        GetNode<Button>("%Continue").Pressed += OnContinue;
        BuildMorningDesk();
        BuildPeopleDesk();
        BuildVisitsDesk();
        BuildCommitmentDesk();
        BuildPerformanceDesk();
        BuildActionDesk();
        BuildDebriefDesk();
        BuildOtherActions();
        BuildBpaDesk();
        BuildPrepDesk();
        BuildSaveControls();
        BuildMapControls();
        BuildFrontDoor();
        BuildDayPlayback();
        BuildEndDayCheck();
        RefreshDisplay("No scenario loaded.");
        PopulateScenarios();
        if (OS.GetCmdlineUserArgs().Contains("--export-smoke-test"))
            Tests.ExportSmokeCheck.Run(this);
    }

    private void PopulateScenarios()
    {
        var picker = GetNode<OptionButton>("%ScenarioPicker");
        try
        {
            var root = ScenarioRootOverride ?? GamePaths.ScenarioRoot;
            scenarios = ScenarioCatalog.Discover(root);
            foreach (var scenario in scenarios)
                picker.AddItem(scenario.Name);
            picker.ItemSelected += LoadScenario;
            picker.Disabled = scenarios.Count == 0;
            if (scenarios.Count > 0)
            {
                var initial = scenarios.Select(x => x.Name).ToList().IndexOf("_stub");
                picker.Select(initial >= 0 ? initial : 0);
                LoadScenario(picker.Selected);
            }
            else
                GetNode<Label>("%BuildStatus").Text = "No scenario folders found.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            picker.Disabled = true;
            GetNode<Label>("%BuildStatus").Text = $"Cannot list scenarios: {error.Message}";
        }
    }

    private void LoadScenario(long index)
    {
        FinishDayPlayback();
        ResetDeskSelection();
        saveError = null;
        session.Clear();
        ShowPrepChoices();
        Map.ShowScenario(null);
        replays = [];
        GetNode<OptionButton>("%ReplayPicker").Clear();
        try
        {
            var data = ScenarioBoardData.Load(scenarios[(int)index].ScenarioPath);
            Map.ShowScenario(data);
            session.LoadScenario(scenarios[(int)index].ScenarioPath);
            ShowPrepChoices();
            SetReplays(FindReplays(scenarios[(int)index].ScenarioPath));
            RefreshDisplay(DeveloperTools ? "Choose pre-deployment preparations, then Deploy. Replay uses its own recorded preparations." : "Choose your preparations before you deploy.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
            KeyNotFoundException or InvalidOperationException or FormatException)
        {
            RefreshDisplay($"Cannot load scenario: {error.Message}");
        }
    }

    private void OnNextDay()
    {
        if (ReadingTurnover || PlayingDay || AtFrontDoor || session.View is not { Finished: false } view) return;
        if (EndDayDialog.Visible) return;
        var open = view.EndOfDayCheck();
        if (open.Count > 0)
        {
            endDayCheckHash = session.StateHash;
            EndDayDialog.GetLabel().Size = new Vector2(600, 0);
            EndDayDialog.DialogText = "Before you end the day\n\n" + string.Join("\n\n", open);
            EndDayDialog.ResetSize();
            EndDayDialog.PopupCentered(new Vector2I(660, 0));
            EndDayDialog.GetCancelButton().GrabFocus();
            return;
        }
        AdvanceDayFromDesk();
    }

    private void AdvanceDayFromDesk()
    {
        var advanced = AdvanceAndAutosave();
        if (advanced && session.View?.Finished == false) FocusNewMorning();
        RefreshDisplay(advanced ? $"Results through day {session.CompletedDay}." : "Day advance is unavailable: no active scenario or scenario finished.", developerOnly: advanced);
        if (advanced) BeginDayPlayback();
    }

    private void OnReplay()
    {
        FinishDayPlayback();
        var index = GetNode<OptionButton>("%ReplayPicker").Selected;
        if (index < 0 || index >= replays.Length) return;
        try
        {
            ResetDeskSelection();
            session.LoadReplay(replays[index]);
            RefreshDisplay($"Replay loaded: {Path.GetFileNameWithoutExtension(replays[index]).Replace('-', ' ')}. End the day plays day 1.", developerOnly: true);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            RefreshDisplay($"Cannot load replay: {error.Message}");
        }
    }

    private string displayStatus = "";
    private bool displayStatusDeveloperOnly;
    private void RefreshBuildStatus()
    {
        var label = GetNode<Label>("%BuildStatus");
        var status = displayStatusDeveloperOnly && !DeveloperTools ? "" : displayStatus;
        label.Text = (DeveloperTools && session.View is not null ? session.IsReplay ? "Replay · " : "Your decisions · " : "")
            + status + (saveError is null ? "" : $"\nSave failed: {saveError}");
        label.Visible = label.Text.Length > 0;
    }

    private void RefreshDisplay(string status, bool developerOnly = false)
    {
        if (endDayCheckHash is not null && endDayCheckHash != session.StateHash) CancelEndDayCheck();
        var view = session.View;
        var strip = view?.TopStrip();
        var board = view?.MorningBoard();
        displayStatus = status; displayStatusDeveloperOnly = developerOnly; RefreshBuildStatus();
        GetNode<Label>("%DayCounter").Text = strip is null ? "No deployment" : view!.Finished ? $"Day {strip.LastDay} complete" : $"Morning of day {strip.Day}";
        GetNode<Button>("%NextDay").Disabled = view is null || view.Finished;
        GetNode<Button>("%Continue").Disabled = view is null || view.Finished;
        var shop = GetNode<ProgressBar>("%ShopHoursGauge");
        var capacity = strip?.ShopHoursPerDay ?? 0;
        var hours = capacity == 1 ? "hour" : "hours";
        GetNode<Label>("%ShopHoursLabel").Text = strip is null ? "Shop time: —" : strip.ShopHoursQueued == 0
            ? $"{capacity:0.#} shop {hours} a day, none spoken for"
            : $"{strip.ShopHoursQueued:0.#} of {capacity:0.#} {hours} spoken for";
        shop.MaxValue = Math.Max(1, capacity);
        shop.Value = Math.Min(strip?.ShopHoursQueued ?? 0, capacity);
        shop.TooltipText = strip is null ? "Load a scenario." : $"Queued work spans {strip.QueueDays} days at current shop capacity.";
        var cash = GetNode<ProgressBar>("%CashGauge");
        GetNode<Label>("%CashLabel").Text = strip is null ? "Cash: —" : $"Cash: {DeskUi.WholeDollars(strip.CashCents)} of {DeskUi.WholeDollars(strip.CashFullCents)}";
        cash.MaxValue = Math.Max(1, strip?.CashFullCents ?? 0);
        cash.Value = strip?.CashCents ?? 0;
        cash.TooltipText = "Cash on hand relative to the full balance.";
        var conditions = new Dictionary<string, SiteCondition>(StringComparer.Ordinal);
        foreach (var site in Map.Data?.Sites ?? [])
        {
            var lowest = board?.Tiles.Where(c => c.SiteId == site.Id).MinBy(c => c.TodayPermille);
            conditions[site.Id] = lowest?.Status switch
            {
                "ok" => SiteCondition.Full, "degraded" => SiteCondition.Degraded, "down" => SiteCondition.Down,
                "notOpen" => SiteCondition.NotOpen, _ => SiteCondition.Unknown
            };
        }
        Map.ShowConditions(conditions);
        Map.ShowConfidence(view?.LostConfidenceSites ?? []);
        Map.ShowCapabilities(board?.Tiles ?? []);
        Map.ShowMapDay(view?.MapDay());
        GetNode<Label>("%InboxDay").Text = view is null ? "No deployment started" : session.CompletedDay == 0 ? "Your first morning" : $"What happened on day {session.CompletedDay}";
        var records = (board?.Items ?? []).Where(r => DeveloperTools || r.Category != LinesOfSupport.Sim.Views.BoardCategory.Request || r.LinkKind != LinesOfSupport.Sim.Views.BoardLinkKind.Card).ToArray();
        GetNode<RichTextLabel>("%InboxRecords").Text = records.Length == 0 ? "No other news for this day." : string.Join("\n\n", records.Select(r => $"[{r.Category} · {r.Severity}] {r.Text}"));
        GetNode<OptionButton>("%ReplayPicker").Disabled = replays.Length == 0;
        GetNode<Button>("%ReplayButton").Disabled = replays.Length == 0;
        RefreshMorningDesk(view, board);
        RefreshPeople(view);
        RefreshVisits(view, board);
        RefreshCommitments(view);
        RefreshPerformance(view);
        RefreshActionDesk(view);
        RefreshBpaDesk(view);
        RefreshDebrief(view);
        RefreshPrepVisibility();
        RefreshSaveControls();
    }
}
