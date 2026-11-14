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
            Require(Rows(shell.DecisionRows).Any(r => !r.GetChildren().OfType<Button>().Any()),
                "An office-wide decision has readable text instead of a dead request link.");
            await Capture("all");
            var requestButton = shell.DecisionRows.FindChildren("*", "Button", true, false).OfType<Button>()
                .First(b => b.HasMeta("decision_card"));
            var card = requestButton.GetMeta("decision_card").AsString();
            var hashBeforeOpen = shell.Session.StateHash;
            requestButton.EmitSignal(Button.SignalName.Pressed);
            Require(tabs.CurrentTab == GameShell.RequestsTab && shell.SelectedCard == card && !shell.MarketOpen,
                "A decision opens its exact request, including after editing a quote.");
            Match(shell.RequestDecisionRows!, shell.Session.View.Decisions(card));
            Require(shell.Session.StateHash == hashBeforeOpen, "Expanding and opening history is pure.");
            var requestScroll = tabs.GetNode<ScrollContainer>("Requests/RequirementPane/Body/Body");
            requestScroll.ScrollVertical = (int)(shell.RequestDecisionRows!.GlobalPosition.Y - requestScroll.GlobalPosition.Y);
            await Capture("request");
            Require(!shell.SendAction(new MeetVendorAction { Day = 3, Vendor = "no-such-vendor" }).Accepted,
                "A refused action is not committed.");
            Require(shell.Session.View.Decisions().Count == count, "Refused actions add no history row.");
            // Drafting and previewing do not enter the accepted-action list either.
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
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

    private async Task Frames() { for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<Control> Rows(Node node) => node.GetChildren().OfType<Control>().Where(c => c.HasMeta("decision_sentence"));
    private static void Match(Node node, IEnumerable<DecisionView> expected)
    {
        var all = expected.ToArray(); var rows = Rows(node).ToArray();
        Require(rows.Select(r => r.GetMeta("decision_sentence").AsString()).SequenceEqual(all.Select(d => d.Sentence)),
            "History keeps exact Sim sentences and newest-first order.");
        for (var i = 0; i < rows.Length; i++)
        {
            var words = string.Join("\n", rows[i].GetChildren().Select(c => c is Button b ? b.Text : c is Label l ? l.Text : ""));
            Require(words.Contains(all[i].Sentence) && words.Contains(all[i].Now) && words.Contains(all[i].Teammate),
                "The visible controls show the exact decision, current status and teammate.");
        }
        Require(rows.Select(r => r.GetMeta("decision_now").AsString()).SequenceEqual(all.Select(d => d.Now))
            && rows.Select(r => r.GetMeta("decision_teammate").AsString()).SequenceEqual(all.Select(d => d.Teammate)),
            "Every row carries its Sim status and teammate.");
    }
}
