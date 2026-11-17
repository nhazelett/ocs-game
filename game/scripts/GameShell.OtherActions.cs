using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer boardActions = null!, coordinationFooter = null!;
    private long coordinationQuantity = 1;
    private readonly Dictionary<string, Button> surveys = [], definitizations = [], terminations = [];
    internal IReadOnlyDictionary<string, Button> SurveyButtons => surveys;
    internal IReadOnlyDictionary<string, Button> DefinitizeButtons => definitizations;
    internal IReadOnlyDictionary<string, Button> TerminateButtons => terminations;
    internal CoordinateAction? PendingCoordination { get; private set; }
    internal Button? CoordinateButton { get; private set; }

    private void BuildOtherActions()
    {
        boardActions = new VBoxContainer { Name = "SupportActions" };
        boardDetails.AddChild(boardActions);
        coordinationFooter = new VBoxContainer { Visible = false }; boardFooter.AddChild(coordinationFooter);
        BuildContractView();
    }

    private void RefreshBoardActions(SessionView? view)
    {
        DeskUi.Clear(boardActions); DeskUi.Clear(coordinationFooter); coordinationFooter.Hide(); surveys.Clear(); PendingCoordination = null; CoordinateButton = null;
        if (view is null) return;
        if (session.IsReplay) boardActions.AddChild(DeskUi.Label("Taking an action takes control and stops future replay commands."));
        boardActions.AddChild(DeskUi.Label("Site investigation"));
        foreach (var island in view.Scenario.Islands)
        {
            var tier = view.KnowledgeTier(island.Id);
            var pending = view.SurveyPending(island.Id);
            var action = new SurveyAction { Day = view.Day, Island = island.Id };
            var button = DeskUi.Button($"Survey {island.Name} · {tier}" + (pending ? " · pending" : ""), () => SendAction(action));
            button.Disabled = view.Finished || pending || tier is "surveyed" or "observed";
            surveys.Add(island.Id, button);
            var reason = DeskUi.ActionReason(boardActions, button);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : pending ? "The survey is already in progress."
                : tier is "surveyed" or "observed" ? "This island has already been surveyed." : null);
            AddActionCost(boardActions, view, action, $"Surveys {island.Name} to improve what the office knows.");
        }
        if (SelectedCapability is null) { boardActions.AddChild(DeskUi.Label("Select a capability tile to coordinate support.")); return; }
        var tile = view.MorningBoard().Tiles.SingleOrDefault(t => t.SiteId + "." + t.CapabilityId == SelectedCapability);
        if (tile is null) return;
        boardActions.AddChild(DeskUi.Label($"Coordinate {tile.Label} to {view.Scenario.Sites.Single(s => s.Id == tile.SiteId).Name}"));
        var sources = view.Scenario.Sites.Where(s => s.Id != tile.SiteId).Select(s => new Pick(s.Id, s.Name)).ToArray();
        if (sources.Length == 0) { boardActions.AddChild(DeskUi.Label("No other source site is available.")); return; }
        var source = sources[0].Id;
        var serial = 1; while (view.Arrangements.Any(a => a.Id == $"player-support-{serial}")) serial++;
        CoordinateButton = DeskUi.Button("Coordinate support", () => { if (PendingCoordination is not null) SendAction(PendingCoordination); });
        Label coordinateReason = null!, coordinateCost = null!;
        void Update()
        {
            PendingCoordination = new CoordinateAction { Day = view.Day, Id = $"player-support-{serial}",
                Site = tile.SiteId, Capability = tile.CapabilityId, FromSite = source, Quantity = coordinationQuantity };
            var preview = view.PreviewAny(PendingCoordination);
            if (coordinateReason is null) return;
            DeskUi.Gate(CoordinateButton, coordinateReason, view.Finished ? DeskUi.FinishedReason : preview.HardStops.FirstOrDefault());
            coordinateCost.Text = ActionCostText(view, PendingCoordination,
                $"Requests {coordinationQuantity} {view.ActionEditor(tile.SiteId, tile.CapabilityId)!.Unit} a day of {tile.Label.ToLowerInvariant()} from {view.NameOf(source)} for {view.NameOf(tile.SiteId)}. Coordination can be withdrawn.", preview);
        }
        AddPick(coordinationFooter, "From site", sources, source, id => { source = id; Update(); });
        coordinationFooter.AddChild(DeskUi.Label($"Quantity ({view.ActionEditor(tile.SiteId, tile.CapabilityId)!.Unit}/day)"));
        var quantityBox = new SpinBox { MinValue = 1, MaxValue = 9007199254740991, Step = 1, Value = coordinationQuantity };
        quantityBox.ValueChanged += value => { coordinationQuantity = checked((long)value); Update(); };
        coordinationFooter.AddChild(quantityBox);
        coordinationFooter.Visible = BoardDetailsVisible;
        coordinateReason = DeskUi.ActionReason(coordinationFooter, CoordinateButton);
        coordinateCost = DeskUi.Label(); coordinateCost.SetMeta("action_cost", nameof(CoordinateAction)); coordinationFooter.AddChild(coordinateCost);
        Update();
    }

    private void AddArrangementActions(VBoxContainer parent, Arrangement arrangement, SessionView view)
    {
        if (arrangement.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) return;
        if (arrangement.Uca is { } uca)
            parent.AddChild(DeskUi.Label("UCA · " + (uca.Definitized ? "definitized" : uca.DefinitizationInWork ? "definitization in work" : uca.ProposalReceived ? "qualifying proposal received" : "awaiting qualifying proposal")));
        if (arrangement.Uca is not { ProposalReceived: true, Definitized: false, DefinitizationInWork: false }) return;
        var funding = view.ActionEditor(arrangement.SiteId, arrangement.CapabilityId)!.Funding;
        var selected = funding.FirstOrDefault(f => f.UsableToday)?.Id;
        Label reason = null!;
        Button button = null!;
        Label cost = null!;
        void Gate()
        {
            var action = new DefinitizeAction { Day = view.Day, Arrangement = arrangement.Id, FundingLine = selected ?? "" };
            var preview = view.PreviewAny(action);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : selected is null ? "Choose usable funding for definitization." : preview.HardStops.FirstOrDefault());
            cost.Text = ActionCostText(view, action, "Puts definitization of this UCA in work.", preview);
        }
        AddPick(parent, "Definitization funding", funding.Select(f => new Pick(f.Id, $"{view.FundingName(f.Id)} · {DeskUi.Money(f.RemainingCents)}", f.UsableToday ? null : "Not usable today")).ToArray(), selected, id => { selected = id; Gate(); });
        button = DeskUi.Button("Definitize " + view.ArrangementName(arrangement.Id), () =>
        {
            if (selected is not null) SendAction(new DefinitizeAction { Day = session.View!.Day, Arrangement = arrangement.Id, FundingLine = selected });
        });
        reason = DeskUi.ActionReason(parent, button); cost = DeskUi.Label(); parent.AddChild(cost); Gate(); definitizations.Add(arrangement.Id, button);
    }
}
