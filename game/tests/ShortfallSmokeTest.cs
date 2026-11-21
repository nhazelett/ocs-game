using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ShortfallSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial"); Step(shell, 3);
            shell.SetProcess(false); shell.DeveloperToggle.ButtonPressed = false;
            var viewport = shell.GetNode<SubViewport>("%BoardViewport");
            var map = viewport.GetNode<SchematicMap>("SchematicMap"); map.SetProcess(false); map.SetMovementProgress(1);
            await Frames(); map.FitMap(); await Frames();
            var hash = shell.Session.StateHash;
            Require(shell.MapHint.Text == "Click a site to see why it is short.", "Short sites advertise their explanations beside Fit map.");
            CheckReasons(); MapCalloutSmokeTest.Check(map); await Capture("tutorial-rest");
            var tabletop = map.Tabletop!;
            var hub = map.Tiles.First(t => t.WhyShort is not null).SiteId;
            Click(CardPoint(hub)); await Frames(); CheckOpen(hub); await Capture("tutorial-open");
            var bounds = tabletop.CalloutCards.Select(c => c.GetRect()).ToArray();
            Click(tabletop.ShortfallCard!.GetRect().GetCenter()); await Frames();
            Require(tabletop.OpenShortfallSite is null, "Clicking an open card closes it.");
            Require(bounds.SequenceEqual(tabletop.CalloutCards.Select(c => c.GetRect())), "Opening the reason never changes resting card placement.");
            Click(map.SiteScreenPoint(hub)); await Frames(); CheckOpen(hub);
            using (var escape = new InputEventKey { Keycode = Key.Escape, Pressed = true }) GetViewport().PushInput(escape, true);
            Require(tabletop.OpenShortfallSite is null, "Escape closes the map detail through viewport input.");
            Click(CardPoint(hub)); await Frames();
            var sea = Enumerable.Range(0, (int)(tabletop.Size.X / 20)).Select(i => new Vector2(10 + i * 20, 10))
                .First(p => tabletop.SiteAt(p) is null);
            Click(sea); Require(tabletop.OpenShortfallSite is null, "Clicking the sea closes the card.");
            var point = CardPoint(hub);
            Mouse(point, true);
            using (var move = new InputEventMouseMotion { Position = point + new Vector2(35, 0), Relative = new Vector2(35, 0), ButtonMask = MouseButtonMask.Left }) viewport.PushInput(move, true);
            using (var move = new InputEventMouseMotion { Position = point, Relative = new Vector2(-35, 0), ButtonMask = MouseButtonMask.Left }) viewport.PushInput(move, true);
            Mouse(point, false);
            Require(tabletop.OpenShortfallSite is null, "A drag out and back cannot become a click.");
            map.FitMap(); await Frames();
            point = CardPoint(hub); Mouse(point, true); Mouse(point + Vector2.One, false); await Frames();
            CheckOpen(hub); Require(hash == shell.Session.StateHash, "All map inspection, closing and dragging is pure presentation.");
            tabletop.CloseShortfall();
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.TodayTab;
            var todayScroll = tabs.GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
            todayScroll.ScrollVertical = 0; await Frames();
            var waterRow = shell.StandingOpenButtons["tutWater"].GetParent().GetParent<Control>();
            todayScroll.ScrollVertical = (int)(waterRow.GlobalPosition.Y - todayScroll.GlobalPosition.Y - 44);
            await Frames(); await Capture("today");
            shell.StandingOpenButtons["tutWater"].EmitSignal(Button.SignalName.Pressed); await Frames();
            var why = shell.FindChildren("*", "Label", true, false).OfType<Label>().Single(l => l.HasMeta("request_why_short"));
            Require(why.IsVisibleInTree() && why.Text == shell.Session.View!.WhereThingsStand().Single(e => e.CardId == "tutWater").WhyShort,
                "The request always displays its exact Sim shortfall sentence.");
            await Capture("request");
            tabs.CurrentTab = GameShell.TodayTab; Click(CardPoint(hub)); await Frames();
            Require(map.Tabletop!.OpenShortfallSite is not null, "A reason is open before advancing.");
            EndDay(shell); Require(shell.PlayingDay && map.Tabletop!.OpenShortfallSite is null, "Day playback closes the reason.");
            shell.FinishDayPlayback(); shell.CloseDaySummary();
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 10);
            shell.DeveloperToggle.ButtonPressed = false; map.FitMap(); map.SetMovementProgress(1); await Frames();
            Require(map.Day!.Day == 10, "Partial-supply coverage uses the actual Spoke B map day 10.");
            MapCalloutSmokeTest.Check(map);
            var spoke = map.Tiles.Where(t => t.WhyShort is not null).GroupBy(t => t.SiteId).OrderByDescending(g => g.Count()).First().Key;
            Click(CardPoint(spoke)); await Frames(); CheckOpen(spoke); await Capture("spoke-open");
            // Switching islands replaces rather than stacks details; view changes still preserve fit.
            foreach (var site in map.Tiles.Where(t => t.WhyShort is not null).Select(t => t.SiteId).Distinct())
            {
                map.Tabletop!.CloseShortfall(); Click(map.SiteScreenPoint(site)); await Frames(); CheckOpen(site);
            }
            var spokeHash = shell.Session.StateHash;
            foreach (var look in new[] { 0, 1, 2 })
            {
                shell.LookPicker.Select(look); shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected, look);
                map.FitMap(); await Frames(); Click(CardPoint(spoke)); await Frames(); CheckOpen(spoke);
                map.ZoomAt(map.GetViewportRect().Size / 2, 1.15f); map.PanBy(new Vector2(12, 8)); await Frames(); CheckOpen(spoke);
                map.ZoomAt(map.SiteScreenPoint(spoke), 2.5f);
                for (var i = 0; i < 24; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                CheckOpen(spoke);
                Require(spokeHash == shell.Session.StateHash, "Zooming and fitting the opened reason preserve the simulation.");
                map.Tabletop!.CloseShortfall(); MapCalloutSmokeTest.Check(map);
            }
            var bundle = LinesOfSupport.Sim.Data.ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var log = LinesOfSupport.Sim.InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-5", "input-log.json"));
            var day16 = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 15);
            var save = Path.Combine(Path.GetTempPath(), "ocs-shortfall-" + Guid.NewGuid() + ".json");
            File.WriteAllText(save, day16.ToSave().ToJson()); shell.LoadSavedGame(save); File.Delete(save);
            shell.DeveloperToggle.ButtonPressed = false; map.FitMap(); map.SetMovementProgress(1); await Frames();
            Require(shell.Session.View!.Day == 16 && shell.Session.StateHash == day16.StateHash(), "The fifth play's exact day-sixteen morning is loaded.");
            var tile16 = shell.Session.View.MorningBoard().Tiles.First(t => t.Status is "down" or "degraded");
            var key16 = tile16.SiteId + "." + tile16.CapabilityId;
            Button Alert() => shell.MorningAlerts.GetChildren().OfType<PanelContainer>().Single(p => p.HasMeta("morning_shortfall")
                && p.GetMeta("morning_shortfall").AsString() == key16).GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single();
            Alert().EmitSignal(Button.SignalName.Pressed); await Frames(); CheckOpen(tile16.SiteId);
            Require(shell.SelectedCapability == key16 && shell.Session.StateHash == day16.StateHash(), "Open site details opens the actual site's card without acting.");
            var openCard = map.Tabletop!.ShortfallCard;
            Alert().EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(map.Tabletop!.ShortfallCard == openCard, "Opening an already-open site leaves its detail open.");
            await Capture("day16-alert-open");
            var card16 = map.Tabletop.RequestButtons.First().Key;
            map.Tabletop.RequestButtons[card16].EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.RequestWorkspace.Visible && shell.SelectedCard == card16 && shell.Session.StateHash == day16.StateHash(), "The opened card's request link is usable and pure.");
            shell.QueueFree(); shell = null; await Frames();
            GD.Print("SHORTFALL_SMOKE_PASS: verbatim reasons on Today/request/map, unchanged resting placement, opaque in-view cards in all looks, real clicks/drag/close/playback and pure inspection."); GetTree().Quit();

            void Mouse(Vector2 point, bool pressed)
            { using var input = new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = pressed }; viewport.PushInput(input, true); }
            void Click(Vector2 point) { Mouse(point, true); Mouse(point, false); }
            Vector2 CardPoint(string site) => map.Tabletop!.SiteStatuses[site].GetParent().GetParent<Control>().GetRect().GetCenter();
            void CheckOpen(string site)
            {
                var table = map.Tabletop!;
                Require(table.OpenShortfallSite == site && table.ShortfallCard is not null, "The clicked site's detail is open: " + site);
                var card = table.ShortfallCard!;
                Require(new Rect2(Vector2.Zero, table.Size - new Vector2(0, table.BottomOcclusion)).Encloses(card.GetRect()),
                    "The complete open card is inside the map: " + card.GetRect() + " in " + table.Size);
                var island = map.Data!.Sites.Single(s => s.Id == site).Island;
                var mapBounds = new Rect2(Vector2.Zero, table.Size - new Vector2(0, table.BottomOcclusion));
                Require(table.SolidInventory.Where(s => s.Island == island)
                    .All(s => s.Polygon.All(mapBounds.HasPoint) && !TabletopMap3D.IntersectsSolid(card.GetRect(), s.Polygon)),
                    "The opened card leaves its entire island and red ring visible: " + site);
                var name = table.SiteStatuses[site].GetParent().GetChildren().OfType<Label>().First();
                Require(!card.GetGlobalRect().Intersects(name.GetGlobalRect()), "The site's name remains visible beside its opened card.");
                Require(table.ShortfallTether!.Points[0].IsEqualApprox(map.SiteScreenPoint(site))
                    && !card.GetRect().HasPoint(table.ShortfallTether.Points[0]),
                    "The visible tether reaches the actual site, outside the card.");
                using var style = card.GetThemeStylebox("panel");
                Require(style is StyleBoxFlat paper && paper.BgColor == TabletopTheme.Paper && card.ZIndex > table.CalloutCards.Max(c => c.ZIndex)
                    && table.ShortfallTether!.ZIndex < card.ZIndex && table.ShortfallTether.ZIndex > table.CalloutCards.Max(c => c.ZIndex), "Opaque paper and tether draw above every resting fact.");
                var reasons = card.FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.HasMeta("why_short")).ToArray();
                var tiles = map.Tiles.Where(t => t.SiteId == site && t.WhyShort is not null).ToArray();
                Require(reasons.Length == tiles.Length, "Every short capability has one explanation.");
                foreach (var tile in tiles)
                {
                    var reason = reasons.Single(l => l.GetMeta("why_short").AsString() == tile.CapabilityId);
                    var capability = card.FindChildren("*", "Label", true, false).OfType<Label>()
                        .Single(l => l.HasMeta("short_capability") && l.GetMeta("short_capability").AsString() == tile.CapabilityId);
                    Require(capability.GetThemeColor("font_color") == TabletopTheme.Alert
                        && reason.GetThemeColor("font_color") == TabletopTheme.Ink
                        && capability.GetIndex() + 1 == reason.GetIndex(), "Each red short capability is immediately followed by its reason in ordinary ink.");
                    Require(reason.Text == tile.WhyShort && reason.GetThemeFontSize("font_size") == 17
                        && card.GetGlobalRect().Encloses(reason.GetGlobalRect()), "Every full Sim reason fits the opaque card at the original readable type size.");
                }
            }
            void CheckReasons()
            {
                var labels = shell!.FindChildren("*", "Label", true, false).OfType<Label>().ToArray();
                foreach (var entry in shell.Session.View!.WhereThingsStand())
                {
                    var request = shell.StandingOpenButtons[entry.CardId];
                    Require(request.Text == entry.Title + (entry.From is null ? "" : " · " + entry.From.DisplayName)
                        + "\nOpen request →\n" + entry.Sentence + (entry.WhyShort is { } why ? "\n" + why : "") + (entry.OrderBy is { } order ? "\n" + order.Sentence : ""),
                        "The request title, state and exact shortfall reason share one clickable frame; supplied requests add no reason.");
                    Require(request.GetParent().GetChildren().Count == 1, "No reason is left between request frames.");
                }
                foreach (var tile in map.Tiles)
                    Require(shell.MorningTiles.GetChildren().OfType<Label>().Any(l => l.Text == tile.WhyShort) == (tile.WhyShort is not null), "Site rows bind Sim reasons too.");
            }
            async Task Capture(string suffix)
            {
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var picture = GetViewport().GetTexture().GetImage();
                Require(picture.GetWidth() == 1280 && picture.GetHeight() == 800, "Review captures match the release viewport.");
                Require(picture.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native shortfall screenshot saved.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("SHORTFALL_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
    private async Task Frames() { for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
}
