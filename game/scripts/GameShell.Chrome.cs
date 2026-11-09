using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private void BuildReadableChrome()
    {
        // Attach generated icons to the theme so checked and unchecked controls stay visible.
        var theme = new Theme();
        foreach (var state in new[] { false, true })
        {
            using var image = Image.CreateEmpty(22, 22, false, Image.Format.Rgba8);
            image.Fill(Colors.Transparent);
            image.FillRect(new Rect2I(2, 2, 18, 18), new Color("dce5de"));
            image.FillRect(new Rect2I(4, 4, 14, 14), new Color("243d4b"));
            if (state)
            {
                for (var i = 0; i < 4; i++) image.FillRect(new Rect2I(6 + i, 10 + i, 2, 2), new Color("f1cc86"));
                for (var i = 0; i < 7; i++) image.FillRect(new Rect2I(9 + i, 13 - i, 2, 2), new Color("f1cc86"));
            }
            var icon = ImageTexture.CreateFromImage(image);
            theme.SetIcon(state ? "checked" : "unchecked", "CheckBox", icon);
            theme.SetIcon(state ? "checked_disabled" : "unchecked_disabled", "CheckBox", icon);
        }
        Theme = theme;
        var cash = GetNode<ProgressBar>("%CashGauge");
        cash.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color("243d4b") });
        cash.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color("83bc98") });
        GetNode<Label>("%ShopHoursLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
        GetNode<Label>("%CashLabel").AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }
}
