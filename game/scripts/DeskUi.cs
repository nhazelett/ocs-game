using Godot;

namespace LinesOfSupport.Game;

internal static class DeskUi
{
    internal static VBoxContainer Panel(Control tab, string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        tab.AddChild(scroll);
        scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var box = new VBoxContainer { Name = "Contents", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(box);
        return box;
    }

    internal static Label Label(string text = "") => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };

    internal static Button Button(string text, Action pressed)
    {
        var button = new Button { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Alignment = HorizontalAlignment.Left, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        button.Pressed += pressed;
        return button;
    }

    internal static void Clear(Node parent)
    {
        foreach (var child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
    }

    internal static string Money(long cents) => "$" + (cents / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
