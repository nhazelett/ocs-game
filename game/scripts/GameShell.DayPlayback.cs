using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private const double PlaybackSeconds = 5;
    private Control playbackOverlay = null!;
    internal Label PlaybackText { get; private set; } = null!;
    internal Button SkipPlaybackButton { get; private set; } = null!;
    internal bool PlayingDay => playbackOverlay?.Visible == true;
    internal MapDayView? PlaybackFacts { get; private set; }
    private double playbackElapsed;

    private void BuildDayPlayback()
    {
        playbackOverlay = new Control { Name = "DayPlayback", Visible = false, MouseFilter = MouseFilterEnum.Stop };
        AddChild(playbackOverlay); playbackOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new PanelContainer(); playbackOverlay.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        panel.OffsetLeft = 16; panel.OffsetRight = -16; panel.OffsetTop = -112; panel.OffsetBottom = -16;
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 16); panel.AddChild(row);
        PlaybackText = DeskUi.Label(""); PlaybackText.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(PlaybackText);
        SkipPlaybackButton = DeskUi.Button("Skip playback", () => FinishDayPlayback()); row.AddChild(SkipPlaybackButton);
        SkipPlaybackButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; SkipPlaybackButton.CustomMinimumSize = new Vector2(180,0);
    }

    private void BeginDayPlayback()
    {
        PlaybackFacts = Map.Day;
        if (PlaybackFacts is null || PlaybackFacts.Day == 0) return;
        RefreshYesterday();
        var day = PlaybackFacts;
        var movements = day.Runs.Count == 0 ? "No deliveries crossed today." : $"{day.Runs.Count} {(day.Runs.Count == 1 ? "delivery crossed" : "deliveries crossed")} today.";
        var held = day.Held.Count == 0 ? "" : day.Held.Any(h => h.Reason == "closed") ? " Cargo waited at a closed route." : " Cargo waited for a scheduled service.";
        var changes = day.Changes.Where(c => c.Known).GroupBy(c => (c.SiteId,c.Kind,c.Permille)).Select(group =>
        {
            var c=group.First();
            var site = Map.Data?.Sites.FirstOrDefault(s => s.Id == c.SiteId)?.Name ?? "A site";
            var names=group.Select(change=>(Map.Tiles.FirstOrDefault(t=>t.CapabilityId==change.CapabilityId)?.Label??"Support").ToLowerInvariant()).Distinct().ToArray();
            var capability=names.Length<2?names[0]:string.Join(", ",names[..^1])+" and "+names[^1];
            return c.Kind switch
            {
                "restored" => $"{site}: {capability} restored.",
                "shortfall" => $"{site}: {capability} short at {c.Permille / 10m:0.#}%.",
                "vendorFailure" => $"{site}: {capability} supplier failure reported.",
                _ => "A site update was reported."
            };
        }).ToArray();
        PlaybackText.Text = $"Day {day.Day} · what happened\n{movements}{held}\n" + (changes.Length==0?"No reported changes in site support.":string.Join(" ",changes));
        playbackElapsed = 0; Map.ReplayMovement(); playbackOverlay.Show(); MoveChild(playbackOverlay, -1);
        GetNode<Button>("%NextDay").Disabled = true; GetNode<Button>("%Continue").Disabled = true;
        RefreshMapCalloutSpace();SkipPlaybackButton.GrabFocus();
    }

    internal void FinishDayPlayback(bool showResults = true)
    {
        if (playbackOverlay is null) return;
        var wasPlaying = PlayingDay;
        playbackOverlay.Hide();RefreshMapCalloutSpace(); playbackElapsed = PlaybackSeconds; Map.SetMovementProgress(1);
        RefreshDayAdvanceControls();
        if (wasPlaying && showResults) ShowDaySummary();
        if (!GetNode<Button>("%NextDay").Disabled) GetNode<Button>("%NextDay").GrabFocus();
    }

    private void RefreshMapCalloutSpace()
    {
        if(Map.Tabletop is not { } tabletop)return;
        var board=GetNode<Control>("%BoardContainer").GetGlobalRect();
        var panel=playbackOverlay.GetChild<PanelContainer>(0).GetGlobalRect();
        tabletop.BottomOcclusion=PlayingDay?Math.Max(0,board.End.Y-panel.Position.Y+6):0;
    }

    internal void TickDayPlayback(double delta)
    {
        if (!PlayingDay) return;
        RefreshMapCalloutSpace();playbackElapsed += delta; Map.SetMovementProgress((float)(playbackElapsed / PlaybackSeconds));
        if (playbackElapsed >= PlaybackSeconds) FinishDayPlayback();
    }
    public override void _Process(double delta) { TickDayPlayback(delta); RefreshMusicContext(); }
}
