using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class MorningScreenFixChecks
{
    internal static async Task Run(GameShell shell)
    {
        var path = Path.Combine(Path.GetTempPath(), "ocs-screen-fixes-" + Guid.NewGuid() + ".json");
        var input = InputLog.Load(Path.Combine(Path.GetDirectoryName(GamePaths.ScenarioRoot)!, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));
        try
        {
            foreach (var day in new[] { 2, 4, 5, 12 })   // D44 (DESK-144): Mills's staff-meeting note is read on day 2
            {
                var save = input with { SavedDay = day, Actions = input.Actions.Where(a => a.Day < day).ToList() };
                File.WriteAllText(path, save.ToJson()); shell.DeveloperToggle.ButtonPressed = false; shell.LoadSavedGame(path);
                await Frames();
                var view = shell.Session.View!; var board = view.MorningBoard(); var hash = shell.Session.StateHash;
                var scroll = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body"); scroll.ScrollVertical = 0;
                Require(shell.MorningAlerts.GetIndex() == shell.MorningHeading.GetIndex() + 1
                    && shell.MorningAlerts.GetIndex() < shell.MorningBrief.GetIndex(), "Current alerts are the first entries under the morning heading.");
                foreach (var tile in board.Tiles.Where(t => t.Status is "down" or "degraded"))
                {
                    var row = shell.MorningAlerts.GetChildren().OfType<PanelContainer>().Single(p => p.GetMeta("morning_shortfall").AsString() == tile.SiteId + "." + tile.CapabilityId);
                    Require(tile.WhyShort is null || PlayScreen.Descendants(row).OfType<Label>().Any(l => l.Text == tile.WhyShort), "Every current shortfall carries its Sim explanation in the first group.");
                }
                Require(shell.MorningHeading.Text.Contains(DeskUi.Count(shell.MorningAlerts.GetChildCount(), "alert")), "The alert count counts the visible alert entries.");
                if (day == 2)
                {
                    var mills = board.New.Single(n => n.Person?.Role == "outgoingCco");
                    Require(!shell.LeadWords.IsVisibleInTree() && !shell.MentorMessages.Visible,
                        "The lead and mentor do not repeat Mills' morning news.");
                    Require(PlayScreen.Descendants(shell.MorningBrief).OfType<Label>().Count(l => l.Text == mills.Text) == 1, "Mills' full note appears exactly once.");
                    var join = PlayScreen.Descendants(shell.MorningBrief).OfType<Button>().Single(b => b.HasMeta("news_staff_meeting"));
                    Require(join.GetParent().GetChildren().OfType<Label>().Any(l => l.HasMeta("action_cost") && l.Text.Contains(view.StaffMeeting.JoinSays, StringComparison.Ordinal)), "The news carries the Sim's verbatim meeting effect, including the daily hour.");
                    await Capture("day3-news");
                    scroll.EnsureControlVisible(join.GetParent().GetChildren().OfType<Label>().Last()); await Frames();
                    await Capture("day3-meeting");
                    var picker = join.GetParent().GetChildren().OfType<AssigneePicker>().Single();
                    Require(picker.Selected == "you", "The news meeting entry defaults to you.");
                    picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
                    join.EmitSignal(Button.SignalName.Pressed);
                    Require(shell.Session.CreateSave().Actions.Last() is JoinStaffMeetingAction { Assignee: "novak" }, "The news entry sends its chosen teammate too.");
                    Require(shell.Session.View!.StaffMeeting.Joining && !PlayScreen.Descendants(shell.MorningBrief).OfType<Button>().Any(b => b.HasMeta("news_staff_meeting")), "Joining from the note queues the real action once and removes its duplicate affordance.");
                }
                else if (day == 4)
                {
                    shell.NavigateBoardItem(new BoardItem("", day, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, "tutForklift", "", []));
                    Require(!PlayScreen.Descendants(shell.RequestColumn).OfType<Label>().Any(l => l.HasMeta("dependency_owner")), "Fully covered tutorial fuel has no usual-owner warning.");
                    shell.TeamButtons.Values.First().EmitSignal(Button.SignalName.Pressed); await Frames();
                    using (var escape = new InputEventKey { Keycode = Key.Escape, Pressed = true }) shell.GetViewport().PushInput(escape, true);
                    await Frames();
                    Require(!shell.TeamPanel.Visible && shell.RequestWorkspace.Visible, "Escape closes the team page on top while preserving the request beneath it.");
                    shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed);
                    shell.PeopleToggle.ButtonPressed = true;
                    var card = view.VendorCards().First(c => c.Rep is not null && c.StillToLearn is null && c.MetDay is null);
                    Pick(shell.PersonContactInput, "vendor:" + card.Rep!.Id);
                    Require(PlayScreen.Descendants(shell.VisitButtons["vendor"].GetParent()).OfType<Label>().Any(l => l.Text.Contains("Their terms are already known.")), "A meeting with a listed vendor admits its terms are already known.");
                }
                else if (day == 5)
                {
                    shell.SelectAwardTarget("tutForklift"); shell.ShowMarket();
                    Pick(shell.VendorInput, "coastalRentals"); Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
                    Require(shell.PriorityInputs.Values.All(b => b.GetThemeColor("font_hover_pressed_color") == TabletopTheme.Ink), "A checked pick-two choice keeps dark ink under the pointer.");
                    await Frames(); shell.AwardScroll.EnsureControlVisible(shell.PriorityInputs["good"]); await Frames();
                    using (var hover = new InputEventMouseMotion { Position = shell.PriorityInputs["good"].GetGlobalRect().GetCenter(), GlobalPosition = shell.PriorityInputs["good"].GetGlobalRect().GetCenter() })
                        shell.GetViewport().PushInput(hover, true);
                    await Capture("day5-pick-two");
                }
                else
                {
                    Require(shell.MorningAlerts.Visible && scroll.GetGlobalRect().Encloses(shell.MorningAlerts.GetGlobalRect()), "The day-12 shortfall is readable at the top of Today, before news and the bar.");
                    var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
                    var words = map.Tabletop!.SiteStatuses["hubSite"];
                    var title = words.GetParent().GetChildren().OfType<Label>().First();
                    Require(board.Tiles.Any(t => t.TodayPermille == 500) && !title.Text.Contains("down"), "A real 50% report never labels the site down by matching the end of 50%.");
                    await Capture("day12-alert");
                }
                if (day != 2) Require(hash == shell.Session.StateHash, "Inspection, Escape, forms and captures do not alter the saved game.");
                GD.Print("MORNING_SCREEN_FIX_DAY_PASS: " + day);
            }
            File.WriteAllText(path, (input with { SavedDay = null }).ToJson()); shell.LoadSavedGame(path); await Frames();
            Require(shell.Session.View!.Finished, "The recorded play reaches its actual debrief.");
            var report = shell.DisplayedDebrief!;
            var gap = report.Gaps.Single(g => g.CapabilityId == "mhe");
            var detail = shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(c => c.HasMeta("gap_detail") && c.GetMeta("gap_detail").AsInt32() == gap.Index);
            detail.Show();
            var trace = PlayScreen.Descendants(detail).OfType<Button>().Single(b => b.HasMeta("gap_trace"));
            trace.EmitSignal(Button.SignalName.Pressed); await Frames();
            var records = PlayScreen.Descendants(detail).OfType<Label>().Where(l => l.HasMeta("cause_record")).ToArray();
            var first = shell.Session.View.Records.Single(r => r.Id == records[0].GetMeta("cause_record").AsString());
            Require(first.Kind == RecordKind.Shortfall, "The trace begins with the actual shortfall, as the harness does.");
            Require(records.Any(l => l.Text == "An earlier cause was not reported to the shop."), "The unreported failure is acknowledged without exposing its hidden text.");
            var purchase = records.FirstOrDefault(l => l.Text.Contains("day 5") && l.Text.Contains("purchase order", StringComparison.OrdinalIgnoreCase));
            Require(purchase is not null, "The trace continues past the unreported failure to the day-5 purchase order.");
            var recordScroll = shell.DebriefPanel.GetParent<ScrollContainer>(); recordScroll.EnsureControlVisible(purchase!); await Frames();
            await Capture("debrief-trace");
        }
        finally { File.Delete(path); }
        async Task Frames() { for (var i = 0; i < 8; i++) await shell.ToSignal(shell.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string name)
        {
            var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--desk108-screenshots"); if (index < 0) return;
            await Frames(); await shell.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = shell.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[index + 1] + "-" + name + ".png") == Error.Ok, "Native review capture saved.");
        }
    }
}
