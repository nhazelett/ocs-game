using Godot;

namespace LinesOfSupport.Game;

internal static class DeskUi
{
    internal static VBoxContainer Panel(Control tab, string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        tab.AddChild(scroll);
        scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var scrollbar = scroll.GetVScrollBar();
        using var track = (StyleBoxFlat)scrollbar.GetThemeStylebox("scroll").Duplicate();
        track.ContentMarginLeft = track.ContentMarginRight = 7;
        scrollbar.AddThemeStyleboxOverride("scroll", track);
        scroll.AddThemeConstantOverride("scrollbar_h_separation", 4);
        var box = new VBoxContainer { Name = "Contents", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(box);
        return box;
    }

    // A single scrolling body and a fixed primary action area; forms never nest scrolls.
    internal static VBoxContainer Pane(Control parent, string name, out VBoxContainer footer)
    {
        var layout = new VBoxContainer { Name = name };
        parent.AddChild(layout); layout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var holder = new Control { Name = "Body", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        layout.AddChild(holder);
        var body = Panel(holder, "Body");
        footer = new VBoxContainer { Name = "Actions" }; layout.AddChild(footer);
        footer.AddThemeConstantOverride("separation", 6);
        return body;
    }

    internal static Label ActionReason(VBoxContainer parent, Button button)
    {
        parent.AddChild(button);
        var label = Label(); label.Visible = false;
        label.AddThemeColorOverride("font_color", TabletopTheme.Alert);
        parent.AddChild(label); return label;
    }

    internal static void Gate(Button button, Label reason, string? blocked)
    {
        button.Disabled = blocked is not null;
        reason.Text = blocked ?? ""; reason.Visible = blocked is not null;
    }

    internal const string FinishedReason = "This deployment has ended.";

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

    internal static string QueueClause(int hours) => hours > 0 ? $", not counting the {hours} hours already queued" : "";

    internal static string ListLabel(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    internal static string WholeDollars(long cents) => "$" + (cents / 100m).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    internal static string Money(long cents) => "$" + (cents / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
