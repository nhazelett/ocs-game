using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DecisionsSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this); Scenario(shell, "tutorial");
            shell.DeveloperToggle.ButtonPressed = false;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var today = tabs.GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
            Require(Rows(shell.DecisionRows).Count() == 0 && !shell.AllDecisionsButton.Visible,
                "Before any action the decision list has an honest empty state.");
            Step(shell, 2);
            Require(shell.SendAction(new MeetVendorAction { Day = 3, Vendor = "sanitation", Assignee = "avery" }).Accepted,
                "A committed office-wide visit supplies a decision without a request.");
            Require(shell.SendAction(new AwardAction { Day = 3, Id = "decisions-water", Instrument = "purchaseOrder", Vendor = "harborBottling",
                Card = "tutWater", Quantity = 4500, DurationDays = 6, FundingLine = "om1", Assignee = "novak" }).Accepted,
                "A real written buy supplies an in-progress request decision.");
            Require(shell.SendAction(new FindFinanceAction { Day = 3 }).Accepted, "Finance work supplies another committed decision.");
            Require(shell.SendAction(new JoinStaffMeetingAction { Day = 3, Attend = true }).Accepted,
                "Joining the meeting supplies a fourth real decision beyond the short list.");
            var expected = shell.Session.View!.Decisions();
            Require(expected.Count > 3, "The fixture has more than the short list.");
            Match(shell.DecisionRows, expected.Take(3));
            Require(shell.DecisionRows.GetParent().GetIndex() > shell.StandingRows.GetParent().GetIndex(),
                "The short decisions list follows Where things stand.");
            var hash = shell.Session.StateHash;
            var count = expected.Count;
            tabs.CurrentTab = GameShell.TodayTab;
            today.ScrollVertical = (int)(shell.DecisionRows.GetParent<Control>().GlobalPosition.Y - today.GlobalPosition.Y);
            await Capture("recent");
            shell.AllDecisionsButton.EmitSignal(Button.SignalName.Pressed);
            Match(shell.DecisionRows, shell.Session.View.Decisions());
            Require(shell.AllDecisionsButton.Text == "Recent decisions", "All decisions expands in the same Today scroll and can return to the short list.");
            Require(Rows(shell.DecisionRows).Any(r => !r.FindChildren("*", "Button", true, false).OfType<Button>().Any()),
                "An office-wide decision has readable text instead of a dead request link.");
            await Capture("all");
            var requestButton = shell.DecisionRows.FindChildren("*", "Button", true, false).OfType<Button>()
                .First(b => b.HasMeta("decision_card"));
            var card = requestButton.GetMeta("decision_card").AsString();
            var hashBeforeOpen = shell.Session.StateHash;
            requestButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.RequestWorkspace.Visible && shell.SelectedCard == card,
                "A decision opens its exact request, including after editing a quote.");
            Match(shell.RequestDecisionRows!, shell.Session.View.Decisions(card));
            Require(shell.Session.StateHash == hashBeforeOpen, "Expanding and opening history is pure.");
            var requestScroll = shell.RequestScroll;
            requestScroll.ScrollVertical = (int)(shell.RequestDecisionRows!.GlobalPosition.Y - requestScroll.GlobalPosition.Y);
            await Capture("request");
            Require(!shell.SendAction(new MeetVendorAction { Day = 3, Vendor = "no-such-vendor" }).Accepted,
                "A refused action is not committed.");
            Require(shell.Session.View.Decisions().Count == count, "Refused actions add no history row.");
            // Drafting and previewing do not enter the accepted-action list either.
            shell.GoToMarket(shell.SelectedCard!);
            Require(shell.MarketOpen, "The real request action opens its quote form.");
            Pick(shell.VendorInput, "harborBottling");
            Pick(shell.InstrumentInput, "purchaseOrder");
            Require(shell.PendingAward is not null, "Choosing actual written-award terms creates a real editable quote.");
            var afterRefusal = shell.Session.StateHash;
            shell.QuantityInput.Value += 1;
            Require(shell.Session.View.Decisions().Count == count && shell.Session.StateHash == afterRefusal,
                "Changing an uncommitted quote creates no decision or state change.");

            var stale = requestButton;
            Step(shell);
            Match(shell.DecisionRows, shell.Session.View.Decisions().Take(3));
            Require(shell.Session.View.Decisions().Any(d => d.Now.StartsWith("Done on day")),
                "The next morning updates status from actual completed work.");
            tabs.CurrentTab = GameShell.TodayTab; stale.EmitSignal(Button.SignalName.Pressed);
            Require(tabs.CurrentTab == GameShell.TodayTab, "A stale history link cannot navigate after a day changes.");
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            hash = shell.Session.StateHash; shell.LoadSavedGame(shell.AutosavePath);
            Match(shell.DecisionRows, shell.Session.View!.Decisions().Take(3));
            Require(shell.Session.StateHash == hash, "History is reconstructed from the accepted log on resume.");
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b");
            Require(Rows(shell.DecisionRows).Count() == 0 && !shell.AllDecisionsButton.Visible,
                "A different deployment clears the old decision list and expanded state.");
            await StopWorkSmokeChecks.Run(this, shell);
            await CheckContractLinks(shell);
            shell.QueueFree(); await Frames();
            GD.Print("DECISIONS_SMOKE_PASS: exact committed history and current status, newest-first short/all, pure request links, drafts/refusals, stale guard and save/reset.");
            GetTree().Quit();

            async Task Capture(string name)
            {
                await Frames();
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots");
                if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var rows = name == "request" ? shell.RequestDecisionRows! : shell.DecisionRows;
                var scroll = name == "request" ? rows.GetParent().GetParent<ScrollContainer>() : today;
                scroll.ScrollVertical += (int)(rows.GlobalPosition.Y - scroll.GlobalPosition.Y - 42);
                await Frames();
                if (name == "all") { scroll.EnsureControlVisible(shell.AllDecisionsButton); await Frames(); }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(rows.GetGlobalRect().Intersects(scroll.GetGlobalRect()), "The captured history is actually inside the visible scroll.");
                Require(rows.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X, "Long decision text wraps inside its single scroll.");
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Native decisions screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError("DECISIONS_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }

    private async Task CheckContractLinks(GameShell shell)
    {
        var bundle = LinesOfSupport.Sim.Data.ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var log = LinesOfSupport.Sim.InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-6", "input-log.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 6);
        var path = shell.AutosavePathOverride + ".day7";
        File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path); File.Delete(path);
        shell.DeveloperToggle.ButtonPressed = false; shell.GoToMarket("tutForklift"); await Frames();
        var coastal = expected.Arrangements.Single(a => a.VendorId == "coastalRentals");
        var links = PlayScreen.Descendants(shell.RequestDecisionRows!).OfType<Button>().Where(b => b.HasMeta("decision_contract")).ToArray();
        Require(links.Length == 1 && links[0].GetMeta("decision_contract").AsString() == coastal.Id
            && expected.View.StatusToday(coastal.Id) == ArrangementStatus.Pending,
            "The signed Coastal buy has its exact contract link before delivery; the declined Harbor call has none.");
        var link = links[0]; shell.RequestScroll.EnsureControlVisible(link); await Frames();
        await Capture("request");
        var scroll = shell.RequestScroll.ScrollVertical; var draft = shell.PendingAward; var hash = shell.Session.StateHash;
        link.EmitSignal(Button.SignalName.Pressed); await Frames();
        Require(shell.ContractView.Visible && shell.SelectedArrangement == coastal.Id && shell.TerminateButtons.ContainsKey(coastal.Id)
            && shell.TerminateButtons[coastal.Id].Text.StartsWith("Cancel this card buy") && shell.Session.StateHash == hash,
            "The request opens the same cancellable contract page as Payments, without acting.");
        await Capture("contract");
        PlayScreen.Descendants(shell.ContractView).OfType<Button>().Single(b => b.Text == "Back to the request").EmitSignal(Button.SignalName.Pressed);
        await Frames();
        Require(!shell.ContractView.Visible && shell.RequestWorkspace.Visible && shell.SelectedCard == "tutForklift"
            && shell.RequestScroll.ScrollVertical == scroll && shell.PendingAward == draft && shell.Session.StateHash == hash,
            "Back returns to the same request, scroll and draft without acting.");
        link.EmitSignal(Button.SignalName.Pressed);
        using (var escape = new InputEventKey { Keycode = Key.Escape, Pressed = true }) shell._UnhandledKeyInput(escape);
        Require(!shell.ContractView.Visible && shell.RequestWorkspace.Visible, "Escape returns from the contract before closing its request.");
        link.EmitSignal(Button.SignalName.Pressed);
        shell.ContractDeskTabs.EmitSignal(TabBar.SignalName.TabSelected, GameShell.TodayTab);
        Require(!shell.ContractView.Visible && !shell.RequestWorkspace.Visible && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
            "A desk tab leaves both contract and request pages.");
        Step(shell); link.EmitSignal(Button.SignalName.Pressed);
        Require(!shell.ContractView.Visible && shell.Session.View!.Day == 8, "A stale request link cannot navigate on a different morning.");
        shell.GoToMarket("tutForklift");
        Require(PlayScreen.Descendants(shell.RequestDecisionRows!).Any(n => n.HasMeta("decision_contract") && n.GetMeta("decision_contract").AsString() == coastal.Id),
            "The same signed buy keeps its contract link once delivery starts.");
        GD.Print("REQUEST_CONTRACT_SMOKE_PASS: exact sixth-play pending and running buy links, no declined link, same contract, request/scroll/draft return, Escape, desk tabs and stale guard.");
        async Task Capture(string suffix)
        {
            await Frames(); var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--contract-screenshots"); if (at < 0) return;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native request-to-contract capture saved.");
        }
    }

    private async Task Frames() { for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<Control> Rows(Node node) => node.GetChildren().OfType<Control>().Where(c => c.HasMeta("decision_sentence"));
    private static void Match(Node node, IEnumerable<DecisionView> expected)
    {
        var all = expected.ToArray(); var rows = Rows(node).ToArray();
        Require(rows.Select(r => r.GetMeta("decision_sentence").AsString()).SequenceEqual(all.Select(d => d.Sentence)),
            "History keeps exact Sim sentences and newest-first order.");
        for (var i = 0; i < rows.Length; i++)
        {
            if (!rows[i].FindChildren("*", "Button", true, false).OfType<Button>().Any())
            {
                var frame = rows[i].GetChildren().OfType<PanelContainer>().Single();
                var block = frame.GetChildren().OfType<Label>().Single().Text;
                Require(block.Contains(all[i].Sentence) && block.Contains(all[i].Now),
                    "A decision without a request and its status share one framed text block.");
            }
            var words = string.Join("\n", rows[i].FindChildren("*", "Control", true, false)
                .Select(c => c is Button b ? b.Text : c is Label l ? l.Text : ""));
            Require(words.Contains(all[i].Sentence) && words.Contains(all[i].Now) && words.Contains(all[i].Teammate, StringComparison.OrdinalIgnoreCase),
                "The visible controls show the exact decision, current status and teammate.");
        }
        Require(rows.Select(r => r.GetMeta("decision_now").AsString()).SequenceEqual(all.Select(d => d.Now))
            && rows.Select(r => r.GetMeta("decision_teammate").AsString()).SequenceEqual(all.Select(d => d.Teammate)),
            "Every row carries its Sim status and teammate.");
    }
}
