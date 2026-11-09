using Godot;
using System.Text.Json;
using System.Globalization;

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
        GetNode<Label>("%GameTitle").Text = ProjectSettings.GetSetting("application/config/name").AsString();
        GetNode<Button>("%NextDay").Pressed += OnNextDay;
        GetNode<Button>("%ReplayButton").Pressed += OnReplay;
        GetNode<Button>("%Continue").Pressed += OnContinue;
        BuildMorningDesk();
        BuildCommitmentDesk();
        BuildPerformanceDesk();
        BuildActionDesk();
        BuildDebriefDesk();
        BuildOtherActions();
        BuildBpaDesk();
        BuildPrepDesk();
        BuildSaveControls();
        BuildFrontDoor();
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
            GetNode<Label>("%BoardCaption").Text = data.Title + " · schematic layout, not geographic positions";
            session.LoadScenario(scenarios[(int)index].ScenarioPath);
            ShowPrepChoices();
            SetReplays(FindReplays(scenarios[(int)index].ScenarioPath));
            RefreshDisplay(DeveloperTools ? "Choose pre-deployment preparations, then Deploy. Replay uses its own recorded preparations." : "Choose your preparations before you deploy.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
            KeyNotFoundException or InvalidOperationException or FormatException)
        {
            if (Map.Data is null) GetNode<Label>("%BoardCaption").Text = "Scenario could not be displayed.";
            RefreshDisplay($"Cannot load scenario: {error.Message}");
        }
    }

    private void OnNextDay()
    {
        var advanced = AdvanceAndAutosave();
        RefreshDisplay(advanced ? $"Results through day {session.CompletedDay}." : "Day advance is unavailable: no active scenario or scenario finished.");
    }

    private void OnReplay()
    {
        var index = GetNode<OptionButton>("%ReplayPicker").Selected;
        if (index < 0 || index >= replays.Length) return;
        try
        {
            ResetDeskSelection();
            session.LoadReplay(replays[index]);
            RefreshDisplay($"Replay loaded: {Path.GetFileNameWithoutExtension(replays[index]).Replace('-', ' ')}. Next day plays day 1.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            RefreshDisplay($"Cannot load replay: {error.Message}");
        }
    }

    private void RefreshDisplay(string status)
    {
        var view = session.View;
        var strip = view?.TopStrip();
        var board = view?.MorningBoard();
        GetNode<Label>("%BuildStatus").Text = (view is null ? "" : session.IsReplay ? "Replay · " : "Your decisions · ") + status + (saveError is null ? "" : $"\nSave failed: {saveError}");
        GetNode<Label>("%DayCounter").Text = strip is null ? "No scenario" : view!.Finished ? $"Day {strip.LastDay} complete" : $"Day {strip.Day} (next)";
        GetNode<Button>("%NextDay").Disabled = view is null || view.Finished;
        GetNode<Button>("%Continue").Disabled = view is null || view.Finished;
        var shop = GetNode<ProgressBar>("%ShopHoursGauge");
        var capacity = strip?.ShopHoursPerDay ?? 0;
        GetNode<Label>("%ShopHoursLabel").Text = strip is null ? "Shop: —" : $"Shop: {strip.ShopHoursQueued} h queued / {capacity} h/day";
        shop.MaxValue = Math.Max(1, capacity);
        shop.Value = Math.Min(strip?.ShopHoursQueued ?? 0, capacity);
        shop.TooltipText = strip is null ? "Load a scenario." : $"Queued work spans {strip.QueueDays} days at current shop capacity.";
        var cash = GetNode<ProgressBar>("%CashGauge");
        GetNode<Label>("%CashLabel").Text = strip is null ? "Cash: —" : $"Cash: ${(strip.CashCents / 100m).ToString("N2", CultureInfo.InvariantCulture)}";
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
        GetNode<Label>("%InboxDay").Text = view is null ? "No scenario loaded" : $"Day {session.CompletedDay} results · next day {(view.Finished ? "complete" : view.Day.ToString())}";
        var records = board?.Items ?? [];
        GetNode<RichTextLabel>("%InboxRecords").Text = records.Count == 0 ? "No records for this day." : string.Join("\n\n", records.Select(r => $"[{r.Category} · {r.Severity}] {r.Text}"));
        GetNode<OptionButton>("%ReplayPicker").Disabled = replays.Length == 0;
        GetNode<Button>("%ReplayButton").Disabled = replays.Length == 0;
        RefreshMorningDesk(view, board);
        RefreshCommitments(view);
        RefreshPerformance(view);
        RefreshActionDesk(view);
        RefreshBpaDesk(view);
        RefreshDebrief(view);
        RefreshPrepVisibility();
        RefreshSaveControls();
    }
}
