using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal const int TodayTab = 0, RequestsTab = 1, VendorsTab = 2, PaymentsTab = 3, InboxTab = 4, DebriefTab = 5;
    internal bool MarketOpen => GetNode<Control>("%DeskTabs/Requests/Market").Visible;

    internal void ShowMarket()
    {
        GetNode<Control>("%DeskTabs/Requests/RequirementPane").Hide();
        GetNode<Control>("%DeskTabs/Requests/Market").Show();
        GetNode<TabContainer>("%DeskTabs").CurrentTab = RequestsTab;
    }

    internal void ShowRequests()
    {
        GetNode<Control>("%DeskTabs/Requests/Market").Hide();
        GetNode<Control>("%DeskTabs/Requests/RequirementPane").Show();
        RenderRequirementContext(session.View);
        GetNode<TabContainer>("%DeskTabs").CurrentTab = RequestsTab;
    }

    private void BuildReadableChrome()
    {
        using var theme = TabletopTheme.Create();
        Theme = theme;
        GetNode<Button>("%NextDay").ThemeTypeVariation="TabletopPrimary";
        var background = new ColorRect { Color = TabletopTheme.Background, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background); MoveChild(background, 0); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetNode<Label>("%ShopHoursLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
        GetNode<Label>("%CashLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }
}
