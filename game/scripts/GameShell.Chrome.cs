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
        foreach(var node in FindChildren("*","Button",true,false).OfType<Button>())node.Alignment=HorizontalAlignment.Left;
        GetNode<Label>("Margin/Layout/WorkArea/BoardPanel/BoardLayout/Heading").ThemeTypeVariation="TabletopHeading";
        GetNode<Label>("%InboxDay").ThemeTypeVariation="TabletopHeading";
        var day=GetNode<Label>("%DayCounter");day.ThemeTypeVariation="TabletopHeading";
        day.AddThemeFontSizeOverride("font_size",22);day.AutowrapMode=TextServer.AutowrapMode.Off;
        day.CustomMinimumSize=new Vector2(185,0);
        GetNode<Label>("%GameTitle").CustomMinimumSize=new Vector2(0,48);
        GetNode<Label>("%GameTitle").VerticalAlignment=VerticalAlignment.Center;
        GetNode<Control>("Margin/Layout/TopStrip/Margin/Row/Cash").CustomMinimumSize=new Vector2(250,0);
        var background = new ColorRect { Color = TabletopTheme.Background, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background); MoveChild(background, 0); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetNode<Label>("%ShopHoursLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
        GetNode<Label>("%CashLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }
}
