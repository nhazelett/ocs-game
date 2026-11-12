using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class RequirementSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-requirement-" + Guid.NewGuid());
        try
        {
            shell = Shell(this);
            shell.LookSettingsPathOverride = Path.Combine(temporary, "look.cfg");
            Directory.CreateDirectory(temporary);
            shell.LookPicker.Select(0); shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            var panel = shell.GetNode<TabContainer>("%DeskTabs").GetNode<VBoxContainer>("Requirements/RequirementPane/Body/Body/Contents");
            var actions = new List<PlayerAction>();
            shell.Session.ActionAccepted += actions.Add;
            void Open(string card) => shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request,
                BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
            void Matches(Session s) => Require(shell.Session.StateHash == s.StateHash(), "The UI reaches the exact independent Sim state.");
            void KnownLines(string card)
            {
                var model = shell.Session.View!.RequirementEditor(card)!;
                Require(!panel.FindChildren("*", "CheckBox", true, false).Any(), "Requirement scope has no checkboxes.");
                var shown = panel.GetChildren().OfType<Label>().Where(l => l.HasMeta("requirement_line")).ToArray();
                Require(shown.Select(l => l.GetMeta("requirement_line").AsString()).SequenceEqual(model.Lines.Where(l => l.Known).Select(l => l.Id)),
                    "Only known lines are shown, in Sim order; unknown parts and their warnings stay hidden.");
                foreach (var line in model.Lines.Where(l => l.Known))
                {
                    Require(shown.Single(l => l.GetMeta("requirement_line").AsString() == line.Id).GetMeta("included").AsBool() == line.On,
                        "Each known line says whether it is already in the request.");
                    Require(shell.RequirementAddButtons.ContainsKey(line.Id) == line.CanAdd, "Add exists only for the Sim's CanAdd lines.");
                    if (line.Learned is not null)
                        Require(panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("learned_line")
                            && l.GetMeta("learned_line").AsString() == line.Id && l.Text.Contains(line.Learned))
                            && panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("learned_from")
                                && l.GetMeta("learned_from").AsString() == line.Id && l.Text.Contains(line.LearnedFrom!.WithUnit)),
                            "The learned fact and its speaker are exact.");
                    if (!line.On && line.GapIfOff is not null)
                        Require(panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("gap_line") && l.Text == line.GapIfOff),
                            "A learned missing part retains the exact consequence.");
                }
                Require(shell.RequirementAddButtons.Keys.All(id => model.Lines.Any(l => l.Id == id && l.Known && l.CanAdd)),
                    "Unknown scope has no add control.");
                Require(panel.GetChildren().OfType<Label>().Any(l => l.Text.Contains($"Peak demand: {model.PeakDemandUnits} {model.Unit}")),
                    "Peak demand and units remain visible.");
            }
            void ResumeMatches()
            {
                var resumed = new GameSessionBridge();
                resumed.ResumeScenario(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"), shell.Session.CreateSave());
                Require(resumed.StateHash == shell.Session.StateHash, "Pending visits, learned facts and added scope survive save/resume.");
            }
            Open("req0007"); KnownLines("req0007"); Matches(expected);
            var hash = shell.Session.StateHash;
            var before = expected.View.RequirementEditor("req0007")!;
            Require(shell.RequirementAskButtons.Keys.SequenceEqual(before.WhoToAsk!.Select(a => a.Person.Id)),
                "Who to ask comes from the Sim.");
            Require(before.WhoToAsk!.All(option => shell.RequirementAskButtons[option.Person.Id].Text == $"Ask {option.Person.DisplayName}")
                && panel.GetChildren().OfType<Label>().Any(l => l.Text == "A request is often missing something. These people may know."),
                "Before the first ask, the button names the person and the request explains why to ask.");
            var askAction = new AskAboutAction { Day = expected.Day, Card = "req0007", Person = "ruiz" };
            var preview = expected.View.PreviewVisit(askAction);
            Require(panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("ask_preview") && l.Text.Contains(preview.ShopHoursMin.ToString())
                && l.Text.Contains($"day {preview.EarliestStartMin}")), "Visit hours and timing come from the Sim preview.");
            Require(hash == shell.Session.StateHash, "Reading the request and visit preview changes no state.");
            var ask = shell.RequirementAskButtons["ruiz"];
            Require(expected.Apply(askAction).Accepted, "Independent ask is accepted.");
            ask.EmitSignal(Button.SignalName.Pressed); Matches(expected);
            Require(actions.Last() is AskAboutAction a && a == askAction && shell.RequirementAskButtons["ruiz"].Disabled
                && shell.RequirementAskButtons["ruiz"].Text == "Visit in progress", "Ask submits the exact typed action and shows pending.");
            hash = shell.Session.StateHash; var count = actions.Count;
            ask.EmitSignal(Button.SignalName.Pressed);
            Require(hash == shell.Session.StateHash && count == actions.Count, "A queued stale click cannot duplicate the visit.");
            ResumeMatches(); Step(shell); expected.AdvanceDay(); Matches(expected); KnownLines("req0007"); ResumeMatches();
            Require(shell.RequirementAskButtons["ruiz"].Disabled && expected.View.RequirementEditor("req0007")!.WhoToAsk![0].Asked,
                "A completed visit is shown as already asked.");
            foreach (var line in new[] { "operator", "maintenance" })
            {
                var add = shell.RequirementAddButtons[line];
                var addAction = new AddLineAction { Day = expected.Day, Card = "req0007", Line = line };
                Require(expected.Apply(addAction).Accepted, "Independent addition is accepted.");
                add.EmitSignal(Button.SignalName.Pressed); Matches(expected);
                Require(actions.Last() == addAction && !shell.RequirementAddButtons.ContainsKey(line), "Add sends the exact typed action once.");
                hash = shell.Session.StateHash; count = actions.Count; add.EmitSignal(Button.SignalName.Pressed);
                Require(hash == shell.Session.StateHash && count == actions.Count, "A stale Add click cannot add twice.");
            }
            KnownLines("req0007"); ResumeMatches();
            Require(!panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("requirement_line") && l.GetMeta("requirement_line").AsString() == "fuelSupply")
                && panel.GetChildren().OfType<Button>().Any(b => b.HasMeta("dependency_visit")), "Spoke B fuel stays undiscovered; the J4 shortcut remains.");
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.CurrentActionEditor?.CardId == "req0007" && shell.CurrentActionEditor.SelectedLines.Contains("operator")
                && shell.CurrentActionEditor.SelectedLines.Contains("maintenance"), "Go to market uses the updated request scope.");

            // A customer who knows nothing does not reveal the CE's facts, and old card callbacks cannot act elsewhere.
            Open("req0012"); var stale = shell.RequirementAskButtons["okafor"];
            Open("req0007"); hash = shell.Session.StateHash; stale.EmitSignal(Button.SignalName.Pressed);
            Require(hash == shell.Session.StateHash, "A stale callback for another selected request does nothing.");
            Open("req0012");
            shell.RequirementAskButtons["okafor"].EmitSignal(Button.SignalName.Pressed); Step(shell);
            KnownLines("req0012");
            Require(shell.RequirementAddButtons.Count == 0 && shell.RequirementAskButtons["okafor"].Disabled
                && !shell.RequirementAskButtons["ruiz"].Disabled, "Asking someone who knows nothing leaves the knowledgeable person available.");

            // The tutorial is the gate path: operators are learned from Dunn, then deliberately added.
            Scenario(shell, "tutorial"); Step(shell, 3); Open("tutForklift"); KnownLines("tutForklift");
            Require(!shell.RequirementAddButtons.ContainsKey("operator"), "The tutorial never exposes an unlearned operators control.");
            await Capture("request", "--request-screenshot");
            shell.RequirementAskButtons["dunn"].EmitSignal(Button.SignalName.Pressed); Step(shell); Open("tutForklift");
            KnownLines("tutForklift");
            Require(shell.RequirementAddButtons.ContainsKey("operator"), "Dunn's completed visit reveals the operators.");
            await Capture("learned", "--learned-screenshot", shell.RequirementAddButtons["operator"]);
            shell.RequirementAddButtons["operator"].EmitSignal(Button.SignalName.Pressed); KnownLines("tutForklift");

            // Exact provenance for both an attended and a missed meeting.
            Scenario(shell, "tutorial"); Step(shell, 8); Open("tutWaste"); HeardAt(false);
            Scenario(shell, "tutorial"); Pick(shell.PersonContactInput, "meeting");
            shell.VisitButtons["meeting"].EmitSignal(Button.SignalName.Pressed); Step(shell, 6); Open("tutWaste"); HeardAt(true);
            while (!shell.Session.View!.Finished) Step(shell);
            Open("tutForklift");
            Require(shell.RequirementAskButtons.Values.All(b => b.Disabled) && shell.RequirementAddButtons.Values.All(b => b.Disabled),
                "Finished games disable requirement actions.");
            hash = shell.Session.StateHash;
            foreach (var button in shell.RequirementAskButtons.Values.ToArray()) button.EmitSignal(Button.SignalName.Pressed);
            Require(hash == shell.Session.StateHash, "Forced finished-game actions do nothing.");

            // Taking control from an authored replay logs only the player's ask, not future authored orders.
            Scenario(shell, "spoke-b"); Replay(shell); Open("req0007");
            shell.RequirementAskButtons["ruiz"].EmitSignal(Button.SignalName.Pressed);
            Require(!shell.Session.IsReplay && shell.Session.CreateSave().Actions.Last() is AskAboutAction, "Asking takes control from replay.");
            var controlled = Session.Create(bundle, 1); controlled.Apply(new AskAboutAction { Day = 1, Card = "req0007", Person = "ruiz" });
            Step(shell); controlled.AdvanceDay(); Matches(controlled);

            File.Delete(shell.AutosavePath); shell.QueueFree(); shell = null; await Frames();
            GC.Collect(); GC.WaitForPendingFinalizers(); await Frames();
            GD.Print("REQUIREMENT_SMOKE_PASS: hidden unknown scope, exact visits and additions, duplicate guards, save/resume, J4, market scope, meeting provenance and replay takeover.");
            GetTree().Quit();

            void HeardAt(bool attended)
            {
                var model = shell!.Session.View!.RequirementEditor("tutWaste")!;
                Require(model.HeardAt is not null && model.HeardAt.Contains(attended ? "was in the room" : "was not in the room")
                    && panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("heard_at") && l.Text == model.HeardAt),
                    "The request shows the Sim's exact staff-meeting source sentence.");
            }
            async Task Capture(string suffix, string legacyFlag, Control? focus = null)
            {
                var args = OS.GetCmdlineUserArgs();
                var prefixAt = Array.IndexOf(args, "--screenshots");
                var at = prefixAt >= 0 ? prefixAt : Array.IndexOf(args, legacyFlag);
                if (at < 0) return;
                Require(at + 1 < args.Length, "The screenshot option needs an output path.");
                var path = args[at + 1] + (prefixAt >= 0 ? "-" + suffix + ".png" : "");
                var stateBefore = shell!.Session.StateHash;
                Require(shell.Session.View!.RequirementEditor("tutForklift")!.Unit == "forklifts"
                    && shell.TeamBars.Count == 3, "Gate captures use plain forklift units and the three-person tutorial team.");
                shell!.DeveloperToggle.ButtonPressed = false; shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 1;
                var scroll = (ScrollContainer)panel.GetParent(); scroll.ScrollVertical = 0;
                await Frames(); if (focus is not null) scroll.EnsureControlVisible(focus); await Frames();
                Require(panel.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X
                    && panel.GetGlobalRect().Position.X >= scroll.GetGlobalRect().Position.X,
                    "The long request title cannot widen or horizontally scroll the request form beyond its viewport.");
                if (focus is not null) Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()),
                    "The learned action is entirely inside the visible request form.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(path) == Error.Ok, "Native request screenshot saved.");
                Require(shell.Session.StateHash == stateBefore, "Capturing a request changes no simulation state.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"REQUIREMENT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }

    private async Task Frames()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
