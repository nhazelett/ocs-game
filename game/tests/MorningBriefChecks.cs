using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class MorningBriefChecks
{
    internal static async Task Run(GameShell shell)
    {
        var root = Path.GetDirectoryName(GamePaths.ScenarioRoot)!;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-morning-brief-" + Guid.NewGuid() + ".json");
        var deadlines = 0;
        try
        {
            Scenario(shell, "spoke-b"); await Frames();
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                "The first morning after turnover opens on Today too.");
            var notOpen = shell.Session.View!.MorningBoard().Tiles.Where(t => t.OpensDay is not null).ToArray();
            Require(notOpen.Length > 0, "The day-one fixture exercises real sites not yet open.");
            foreach (var tile in notOpen)
            {
                var key = tile.SiteId + "." + tile.CapabilityId;
                var button = shell.MorningTiles.GetChildren().OfType<Button>().Single(b => b.GetMeta("capability").AsString() == key);
                Require(button.Text.Contains($"opens day {tile.OpensDay}") && !button.Text.Contains('%'), "A future site's tile has its opening day and no percent.");
            }
            var selected = notOpen.First();
            shell.NavigateBoardItem(new BoardItem("", 1, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability,
                selected.SiteId + "." + selected.CapabilityId, "", []));
            Require(shell.BoardFocus.Text.Contains($"opens day {selected.OpensDay}") && !shell.BoardFocus.Text.Contains('%'),
                "Selecting a future site keeps the same opening date without inventing a coverage reading.");
            foreach (var (logName, day, screenshot) in new[] {
                ("tutorial-careful", 5, "tutorial-day5"), ("tutorial-careful", 7, "tutorial-day7"), ("spoke-b", 13, "spoke-b-day13") })
            {
                var input = InputLog.Load(Path.Combine(root, "docs", "reviews", "plays", "2026-11-17-review", logName + "-input-log.json"));
                var save = input with { SavedDay = day, Actions = input.Actions.Where(a => a.Day < day).ToList() };
                File.WriteAllText(temporary, save.ToJson());
                var oldScroll = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body");
                oldScroll.ScrollVertical = (int)oldScroll.GetVScrollBar().MaxValue;
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.InboxTab;
                shell.DeveloperToggle.ButtonPressed = false; shell.LoadSavedGame(temporary);
                await Frames();
                Require(oldScroll.ScrollVertical == 0 && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                    "Resuming also opens Today at the top instead of preserving an old scroll position.");
                var view = shell.Session.View!; var board = view.MorningBoard();
                Require(view.Day == day, "The review save resumes on the exact morning.");
                var hash = shell.Session.StateHash;
                var labels = PlayScreen.Descendants(shell.MorningBrief).OfType<Label>().ToArray();
                Require(labels.Where(l => l.HasMeta("news_surface")).Select(l => l.Text).SequenceEqual(board.New.Select(n => n.Text)),
                    "The uncollapsed morning contains every new request, full answer and news sentence in Sim order.");
                Require(labels.Where(l => l.HasMeta("asked_by")).Select(l => l.GetMeta("asked_by").AsString())
                    .SequenceEqual(board.New.Where(n => n.AskedBy is not null).Select(n => n.AskedBy)), "Answers name who asked.");
                Require(shell.MorningBrief.GetParent() == shell.StandingRows.GetParent().GetParent()
                    && shell.MorningBrief.GetIndex() < shell.StandingRows.GetParent().GetIndex() && !shell.BoardDetailsVisible,
                    "The brief precedes standings outside Other updates.");
                Require(shell.MorningHeading.Text.EndsWith($"{board.OpenRequests} open requests"), "The heading counts open requests, not overnight records.");
                Require(!shell.LeadWords.IsVisibleInTree() || shell.LeadWords.Text != "There is no priority message this morning.",
                    "News never follows a no-priority headline.");
                foreach (var entry in view.WhereThingsStand().Where(e => e.OrderBy is not null))
                {
                    Require(shell.StandingOpenButtons[entry.CardId].Text.Contains(entry.OrderBy!.Sentence),
                        "Each known order deadline and its assumed route share their request's frame.");
                    deadlines++;
                }
                foreach (var tile in board.Tiles.Where(t => t.OpensDay is not null))
                {
                    var button = shell.MorningTiles.GetChildren().OfType<Button>().Single(b => b.GetMeta("capability").AsString() == tile.SiteId + "." + tile.CapabilityId);
                    Require(button.Text.Contains($"opens day {tile.OpensDay}") && !button.Text.Contains('%'), "A not-open tile has its opening day and no percent.");
                }
                if (day == 5) Require(labels.Any(l => l.Text.Contains("nobody licensed")) && labels.Any(l => l.Text.Contains("short of forklifts")), "Dunn and the short vendor both reach the morning of day 5.");
                if (day == 7) Require(board.New.First().Kind == NewKind.Request && board.New.First().LinkId == "tutWaste", "Day 7 starts with the new waste request.");
                if (day == 13) Require(labels.Any(l => l.Text.Contains("64") && l.Text.Contains("Spoke C")), "Spoke C's 64 new people reach the morning of day 13.");
                await Frames();
                var scroll = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body");
                scroll.ScrollVertical = 0; await Frames();
                if (day == 5)
                    Require(labels.Where(l => l.HasMeta("news_surface")).All(l => scroll.GetGlobalRect().Encloses(l.GetGlobalRect())),
                        "Dunn's full answer and the vendor shortage fit together at the top of the native morning.");
                await Capture(screenshot);
                if (day == 7)
                {
                    var request = board.New.First();
                    PlayScreen.Descendants(shell.MorningBrief).OfType<Button>().Single(b => b.GetMeta("news_link").AsString() == request.LinkId).EmitSignal(Button.SignalName.Pressed);
                    Require(shell.SelectedCard == request.LinkId && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.RequestsTab,
                        "The new request opens its exact card without changing the simulation.");
                    if (view.WhereThingsStand().Single(e => e.CardId == request.LinkId).OrderBy is { } order)
                        Require(PlayScreen.Descendants(shell.GetNode("%DeskTabs/Requests/RequirementPane")).OfType<Label>().Any(l => l.Text == order.Sentence), "The request itself repeats its exact order deadline.");
                }
                shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed);
                var summary = shell.DisplayedDaySummary!;
                var summaryLabels = PlayScreen.Descendants(shell.DaySummaryContents).OfType<Label>().ToArray();
                Require(summaryLabels.Where(l => l.HasMeta("summary_kind") && l.GetMeta("summary_kind").AsString() == "said").Select(l => l.Text)
                    .SequenceEqual(summary.Said.Select(n => n.Text)), "Results show every answer in full and in order.");
                Require(summaryLabels.Where(l => l.HasMeta("summary_kind") && l.GetMeta("summary_kind").AsString() == "news").Select(l => l.Text)
                    .SequenceEqual(summary.News.Select(n => n.Text)), "Results show all the day's news in order.");
                if (day == 5) await Capture("tutorial-day4-results");
                shell.CloseDaySummary(); await Frames();
                Require(scroll.ScrollVertical == 0 && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                    "Closing yesterday opens the morning at its top.");
                Require(hash == shell.Session.StateHash, "Reading and following brief links changes no Sim state.");
                scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.InboxTab;
                EndDay(shell); shell.FinishDayPlayback(); shell.CloseDaySummary(); await Frames();
                Require(scroll.ScrollVertical == 0 && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                    "The next morning opens on Today at the top even after another tab and a scrolled morning.");
            }
            Require(deadlines > 0, "The review fixtures exercise at least one real order deadline.");
        }
        finally { File.Delete(temporary); }

        async Task Frames() { for (var i = 0; i < 8; i++) await shell.ToSignal(shell.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string name)
        {
            await Frames();
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--brief-screenshots");
            if (at < 0) return;
            await shell.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = shell.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Native morning brief saved.");
        }
    }
}
