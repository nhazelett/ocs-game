using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
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
