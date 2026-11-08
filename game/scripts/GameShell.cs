using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell : Control
{
    private readonly GameSessionBridge session = new();

    public override void _Ready()
    {
        GetNode<Label>("%GameTitle").Text = ProjectSettings.GetSetting("application/config/name").AsString();
        GetNode<Button>("%NextDay").Pressed += OnNextDay;
        RefreshStrip();
    }

    private void OnNextDay()
    {
        if (!session.RequestNextDay())
            GetNode<Label>("%BuildStatus").Text = "Day advance is unavailable until a scenario is connected.";
        RefreshStrip();
    }

    private void RefreshStrip()
    {
        GetNode<Label>("%DayCounter").Text = $"Day {session.Day}";
        GetNode<Label>("%ShopHoursLabel").Text = session.ShopHours is int hours ? $"Shop hours: {hours}" : "Shop hours: —";
        GetNode<Label>("%CashLabel").Text = session.CashCents is long cents ? $"Cash: {cents / 100m:C}" : "Cash: —";
    }
}
