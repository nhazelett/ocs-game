using Godot;
using System.Text;

namespace LinesOfSupport.Game.Tests;

// Read the presented scene tree, never the session, scenarios, metadata or hidden controls.
internal sealed class PlayScreen(GameShell shell)
{
    internal sealed record Entry(Control Control, string Pane, string Kind, string Text, Rect2 Rect,
        bool Disabled = false, int Index = -1);

    internal static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren(includeInternal: true))
        {
            yield return child;
            foreach (var next in Descendants(child)) yield return next;
        }
    }

    private IEnumerable<Node> Roots()
    {
        var windows = Descendants(shell).OfType<Window>().Where(w => w.Visible).ToArray();
        if (windows.Length > 0) { yield return windows[^1]; yield break; }
        // Opaque full-screen pages cover the desk, even though its children remain Visible.
        var cover = shell.GetChildren().OfType<PanelContainer>()
            .Where(c => c.IsVisibleInTree() && c.GetGlobalRect().Size.X >= 1200 && c.GetGlobalRect().Size.Y >= 780)
            .OrderBy(c => c.ZIndex).LastOrDefault();
        if (cover is not null)
        {
            yield return cover;
            if (cover.ZIndex <= 15) yield return shell.GetNode("Navigation");
        }
        else yield return shell;
    }

    private static Rect2 ScreenRect(Control control)
    {
        var rect = control.GetGlobalRect();
        Viewport viewport = control.GetViewport();
        while (viewport is SubViewport sub && sub.GetParent() is SubViewportContainer holder)
        {
            var scale = holder.Size / (Vector2)sub.Size;
            rect = new Rect2(holder.GlobalPosition + rect.Position * scale, rect.Size * scale);
            viewport = holder.GetViewport();
        }
        if (viewport is Window window && window.GetParent() is not null) rect.Position += window.Position;
        return rect;
    }

    private static Rect2 VisibleRect(Control control)
    {
        if (!control.IsVisibleInTree() || control.Modulate.A == 0) return new Rect2();
        var rect = ScreenRect(control).Intersection(new Rect2(0, 0, 1280, 800));
        for (var node = control.GetParent(); node is not null; node = node.GetParent())
        {
            if (node is Control { ClipContents: true } clip) rect = rect.Intersection(ScreenRect(clip));
            if (node is SubViewport sub && sub.GetParent() is SubViewportContainer holder) rect = rect.Intersection(ScreenRect(holder));
            if (node is CanvasItem canvas && !canvas.IsVisibleInTree()) return new Rect2();
        }
        return rect;
    }

    private static string Pane(Control control)
    {
        for (Node? node = control; node is not null; node = node.GetParent())
        {
            if (node is SubViewport) return "Map";
            if (node is TabBar) return "Desk tabs";
            if (node is Control tab && node.GetParent() is TabContainer tabs)
            {
                var index = tabs.GetTabIdxFromControl(tab);
                if (index >= 0) return tabs.GetTabTitle(index);
            }
            if (node is Control && node.Name.ToString() is "PrepScreen" or "Turnover" or "FrontDoor" or "Team")
                return node.Name.ToString() switch { "PrepScreen" => "Preparation", "FrontDoor" => "Title", _ => node.Name.ToString() };
            if (node is Window window && node.GetParent() is not null) return window is PopupMenu ? "Choices" : "Dialog";
        }
        return "Desk";
    }

    private static string FieldName(LineEdit field)
    {
        if (!string.IsNullOrWhiteSpace(field.PlaceholderText)) return field.PlaceholderText;
        // Use the same caption a player reads beside the field, never an internal node name.
        for (Node node = field; node.GetParent() is Control parent; node = parent)
        {
            var previous = parent.GetChildren().TakeWhile(n => n != node).OfType<Label>()
                .LastOrDefault(label => VisibleRect(label).Size.Y > 2);
            if (previous is not null) return previous.Text.Trim().TrimEnd(':');
            if (parent is ScrollContainer) break;
        }
        return "Name";
    }

    private static string VisibleLabel(Label label, Rect2 clipped)
    {
        if (clipped.Encloses(ScreenRect(label))) return label.Text;
        // Label text can extend below a scroll clip. Return only its visible wrapped lines.
        using var paragraph = new TextParagraph { Width = label.Size.X,
            BreakFlags = TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive };
        paragraph.AddString(label.Text, label.GetThemeFont("font"), label.GetThemeFontSize("font_size"));
        var lines = new List<string>();
        var height = label.GetLineHeight();
        for (var i = 0; i < paragraph.GetLineCount(); i++)
        {
            var y = ScreenRect(label).Position.Y + i * height;
            if (y < clipped.Position.Y - 1 || y + height > clipped.End.Y + 1) continue;
            var range = paragraph.GetLineRange(i);
            // Godot's ranges use Unicode codepoints, while .NET string offsets use UTF-16.
            lines.Add(string.Concat(label.Text.EnumerateRunes().Skip(range.X).Take(range.Y - range.X)));
        }
        return string.Join("\n", lines);
    }

    private static string ButtonWords(Button button, Rect2 clipped)
    {
        var bounds = ScreenRect(button);
        var font = button.GetThemeFont("font");
        var size = button.GetThemeFontSize("font_size");
        var style = button.GetThemeStylebox("normal");
        var width = button.Size.X - style.GetMinimumSize().X;
        if (button.Icon is { } icon)
            width -= Math.Min(button.GetThemeConstant("icon_max_width"), icon.GetWidth()) + button.GetThemeConstant("h_separation");
        if (button is OptionButton) width -= 24;
        if (button.AutowrapMode == TextServer.AutowrapMode.Off && button.ClipText
            && font.GetStringSize(button.Text, fontSize: size).X > width)
        {
            var prefix = "";
            foreach (var rune in button.Text.EnumerateRunes())
            {
                if (font.GetStringSize(prefix + rune + "…", fontSize: size).X > width) break;
                prefix += rune.ToString();
            }
            return prefix + "…";
        }
        if (clipped.Encloses(bounds)) return button.Text;
        using var paragraph = new TextParagraph { Width = width,
            BreakFlags = button.AutowrapMode == TextServer.AutowrapMode.Off ? TextServer.LineBreakFlag.Mandatory
                : TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive };
        paragraph.AddString(button.Text, font, size);
        var height = font.GetHeight(size);
        var top = bounds.Position.Y + (bounds.Size.Y - paragraph.GetLineCount() * height) / 2;
        var words = new List<string>();
        for (var i = 0; i < paragraph.GetLineCount(); i++)
        {
            var y = top + i * height;
            if (y < clipped.Position.Y - 1 || y + height > clipped.End.Y + 1) continue;
            var range = paragraph.GetLineRange(i);
            words.Add(string.Concat(button.Text.EnumerateRunes().Skip(range.X).Take(range.Y - range.X)));
        }
        return string.Join("\n", words);
    }

    private static Rect2 MenuItemRect(PopupMenu menu, int index)
    {
        var font = menu.GetThemeFont("font");
        var size = menu.GetThemeFontSize("font_size");
        var separation = menu.GetThemeConstant("v_separation");
        var panel = menu.GetThemeStylebox("panel");
        var scroll = Descendants(menu).OfType<ScrollContainer>().FirstOrDefault();
        var y = panel.GetMargin(Side.Top) - (scroll?.ScrollVertical ?? 0);
        for (var i = 0; i <= index; i++)
        {
            using var icon = menu.GetItemIcon(i);
            var iconHeight = icon?.GetHeight() ?? 0;
            var max = menu.GetItemIconMaxWidth(i);
            if (icon is not null && max > 0 && icon.GetWidth() > max) iconHeight = iconHeight * max / icon.GetWidth();
            var height = Math.Max(font.GetHeight(size), iconHeight) + separation;
            if (i == index) return new Rect2(0, y, menu.Size.X, height);
            y += height;
        }
        return new Rect2();
    }

    internal List<Entry> Read()
    {
        var entries = new List<Entry>();
        foreach (var root in Roots())
        {
            if (root is PopupMenu menu)
            {
                for (var i = 0; i < menu.ItemCount; i++)
                {
                    if (menu.IsItemSeparator(i)) continue;
                    // PopupMenu owns its input and scrolling; only on-screen items qualify.
                    var rect = MenuItemRect(menu, i);
                    if (!rect.Intersects(new Rect2(Vector2.Zero, menu.Size))) continue;
                    rect = rect.Intersection(new Rect2(Vector2.Zero, menu.Size));
                    rect.Position += menu.Position;
                    entries.Add(new Entry(menu.GetParent<Control>(), "Choices", "choice", menu.GetItemText(i), rect,
                        menu.IsItemDisabled(i), i));
                }
                var scrolling = Descendants(menu).OfType<ScrollContainer>().FirstOrDefault();
                if (scrolling is not null) entries.Add(new Entry(scrolling, "Choices", "scroll", "Choices", new Rect2(Vector2.Zero, menu.Size)));
                continue;
            }
            foreach (var control in Descendants(root).Prepend(root).OfType<Control>())
            {
                var rect = VisibleRect(control);
                if (rect.Size.X < 2 || rect.Size.Y < 2) continue;
                var pane = Pane(control);
                switch (control)
                {
                    case TabBar bar:
                        for (var i = 0; i < bar.TabCount; i++)
                        {
                            if (bar.IsTabHidden(i)) continue;
                            var tabRect = bar.GetTabRect(i); tabRect.Position += ScreenRect(bar).Position;
                            tabRect = tabRect.Intersection(rect);
                            if (tabRect.Size.X > 2) entries.Add(new Entry(bar, pane, "tab", bar.GetTabTitle(i), tabRect, bar.IsTabDisabled(i), i));
                        }
                        break;
                    case Button button:
                        entries.Add(new Entry(button, pane, button is OptionButton ? "picker" : "button", ButtonWords(button, rect), rect, button.Disabled));
                        break;
                    case LineEdit field:
                        entries.Add(new Entry(field, pane, "field", FieldName(field), rect, !field.Editable));
                        break;
                    case Label label:
                        var text = VisibleLabel(label, rect);
                        if (!string.IsNullOrWhiteSpace(text)) entries.Add(new Entry(label, pane, "text", text, rect));
                        break;
                    case RichTextLabel rich:
                        // Rich labels in this UI are short; omit partially clipped ones rather than reveal hidden text.
                        if (rect.Encloses(rich.GetGlobalRect())) entries.Add(new Entry(rich, pane, "text", rich.GetParsedText(), rect));
                        break;
                    case ScrollContainer scroll:
                        entries.Add(new Entry(scroll, pane, "scroll", pane, rect));
                        break;
                }
            }
        }
        return entries.OrderBy(e => e.Rect.Position.Y).ThenBy(e => e.Rect.Position.X).ToList();
    }

    internal string Describe(string result)
    {
        var entries = Read();
        var text = new StringBuilder(result + "\n\n");
        foreach (var pane in entries.Select(e => e.Pane).Distinct())
        {
            text.AppendLine("[" + pane + "]");
            foreach (var entry in entries.Where(e => e.Pane == pane))
            {
                var label = entry.Text;
                if (entry.Kind == "text") { text.AppendLine(label); continue; }
                if (entry.Kind == "scroll")
                {
                    var scroll = (ScrollContainer)entry.Control;
                    var bar = scroll.GetVScrollBar();
                    text.AppendLine($"scroll \"{label}\" (up: {scroll.ScrollVertical > 0}, more below: {bar.Value + bar.Page + 1 < bar.MaxValue})");
                    continue;
                }
                var peers = entries.Where(e => e.Kind != "text" && e.Kind != "scroll"
                    && (e.Kind == "field") == (entry.Kind == "field")
                    && e.Text.Equals(label, StringComparison.OrdinalIgnoreCase)).ToList();
                var duplicate = peers.Count > 1;
                text.AppendLine($"{entry.Kind} \"{label}\"{(duplicate ? " #" + (peers.IndexOf(entry) + 1) : "")} [{(entry.Disabled ? "greyed" : "enabled")}]" +
                    (entry.Control is LineEdit edit ? $" = \"{edit.Text}\"" : entry.Control is CheckBox check ? (check.ButtonPressed ? " [checked]" : " [unchecked]") : ""));
            }
            text.AppendLine();
        }
        return text.ToString();
    }
}
