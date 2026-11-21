using Godot;
using System.Text.Json;

namespace LinesOfSupport.Game.Tests;

// A scratch-only test scene. It drives GUI input and copies the game's own autosave;
// there is deliberately no Sim reference or session inspection here.
public partial class PlayDriver : Node
{
    internal sealed record Command(string Id, string Verb, string Label = "", string Value = "", int Occurrence = 1);
    internal GameShell Shell { get; private set; } = null!;
    internal bool TestMode { get; set; }
    private PlayScreen screen = null!;
    private string output = "";
    private int sequence;
    private bool busy;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public override async void _Ready()
    {
        SetProcess(false);
        if (TestMode) return;
        try
        {
            var args = OS.GetCmdlineUserArgs();
            string Arg(string key) => Array.IndexOf(args, key) is var i && i >= 0 && i + 1 < args.Length
                ? args[i + 1] : throw new ArgumentException($"Missing {key}.");
            await Initialize(Arg("--scenario"), long.Parse(Arg("--seed")), Arg("--out"));
            File.WriteAllText(Path.Combine(output, "ready.json"), JsonSerializer.Serialize(new { pid = System.Environment.ProcessId }));
            SetProcess(true);
        }
        catch (Exception error) { GD.PushError($"PLAY_DRIVER_FAIL: {error}"); GetTree().Quit(1); }
    }

    internal async Task Initialize(string scenario, long seed, string directory)
    {
        SetProcess(false);
        output = Path.GetFullPath(directory);
        var repo = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
        if (!Path.IsPathRooted(directory) || output == repo || output.StartsWith(repo + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("--out must be an absolute scratch directory outside the repository.");
        if (scenario is not ("tutorial" or "spoke-b")) throw new ArgumentException("Use tutorial or spoke-b.");
        Directory.CreateDirectory(output);
        if (Directory.EnumerateFiles(output, "*.png").Any() || File.Exists(Path.Combine(output, "ready.json")))
            throw new ArgumentException("Use a fresh output directory for each play.");
        using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
        Shell = scene.Instantiate<GameShell>();
        Shell.AutosavePathOverride = Path.Combine(output, "autosave.json");
        Shell.TeamSettingsPathOverride = Path.Combine(output, "team.cfg");
        Shell.Session.NewGameSeed = seed;
        GetTree().Root.GuiEmbedSubwindows = true;
        AddChild(Shell);
        screen = new PlayScreen(Shell);
        await Settle();
        await Execute(new Command("startup-new", "click", "New game"));
        await Execute(new Command("startup-scenario", "click", scenario == "tutorial" ? "Tutorial" : "Spoke B"));
        await Capture("New game opened through its visible choices. Developer tools are off.");
    }

    public override async void _Process(double delta)
    {
        var request = Path.Combine(output, "request.json");
        if (busy || !File.Exists(request)) return;
        busy = true;
        Command? command = null;
        try
        {
            command = JsonSerializer.Deserialize<Command>(File.ReadAllText(request), JsonOptions)
                ?? throw new InvalidDataException("Empty command.");
            File.Delete(request);
            var result = await Execute(command);
            var paths = await Capture(result);
            var response = JsonSerializer.Serialize(new { id = command.Id, screenshot = paths.png, text = paths.txt });
            File.WriteAllText(Path.Combine(output, "response.json.tmp"), response);
            File.Move(Path.Combine(output, "response.json.tmp"), Path.Combine(output, "response.json"), true);
            if (command.Verb == "quit") GetTree().Quit();
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(output, "response.json"), JsonSerializer.Serialize(new { id = command?.Id, error = error.Message }));
            GD.PushError(error.ToString());
        }
        finally { busy = false; }
    }

    private async Task Settle()
    {
        // Wait for queued layout, focus and popup updates, then for text/geometry to stop changing.
        string? previous = null;
        var stable = 0;
        for (var i = 0; i < 600; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Shell.PlayingDay)
            {
                await ToSignal(GetTree().CreateTimer(.05), SceneTreeTimer.SignalName.Timeout);
                stable = 0; continue;
            }
            var now = screen.Describe("");
            if (now == previous) stable++; else stable = 0;
            previous = now;
            if (i >= 5 && stable >= 3) return;
        }
    }

    internal async Task<string> Execute(Command command)
    {
        var entries = screen.Read();
        var verb = command.Verb.ToLowerInvariant();
        if (verb is "look" or "quit") return verb == "quit" ? "Play ended; the game's input log was saved." : "Current screen.";
        if (verb == "key")
        {
            if (command.Label != "Escape") return "Only key Escape is supported.";
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = false });
            await Settle(); return "Pressed Escape.";
        }
        var kind = verb == "type" ? new[] { "field" } : verb == "scroll" ? new[] { "scroll" } : new[] { "button", "picker", "tab", "choice" };
        // Match complete lines of the already clipped button text, never hidden words or arbitrary substrings.
        var matches = entries.Where(e => (kind.Contains(e.Kind) || (verb == "click" && e.Kind == "text" && e.Pane == "Map"))
            && (e.Text.Equals(command.Label, StringComparison.OrdinalIgnoreCase)
                || (verb == "click" && e.Kind == "button" && e.Text.Split('\n')
                    .Any(line => line.Trim().TrimEnd('→').TrimEnd().Equals(command.Label, StringComparison.OrdinalIgnoreCase))))).ToArray();
        if (command.Occurrence < 1 || command.Occurrence > matches.Length)
        {
            var labels = entries.Where(e => kind.Contains(e.Kind)).Select(e => e.Text).Distinct()
                .OrderByDescending(label => Similarity(label, command.Label)).Take(8);
            return $"No visible {verb} target \"{command.Label}\" #{command.Occurrence}. Nothing changed. Nearest labels on screen: {string.Join("; ", labels)}";
        }
        var target = matches[command.Occurrence - 1];
        if (target.Disabled) return $"\"{target.Text}\" is greyed. Nothing changed; read its visible reason below.";
        if (verb == "scroll")
        {
            if (command.Value is not ("up" or "down")) return "Scroll direction must be up or down. Nothing changed.";
            var scroll = (ScrollContainer)target.Control;
            scroll.ScrollVertical += (int)(scroll.Size.Y * .75f) * (command.Value == "down" ? 1 : -1);
        }
        else if (verb == "click")
        {
            // No back door to developer tools during a blind play.
            if (target.Control == Shell.DeveloperToggle) return "Developer tools are unavailable to the play driver. Nothing changed.";
            await MouseClick(target);
        }
        else if (verb == "type")
        {
            var field = (LineEdit)target.Control;
            await MouseClick(target);
            field.GrabFocus(); field.DeleteText(0, field.Text.EnumerateRunes().Count()); field.CaretColumn = 0;
            field.InsertTextAtCaret(command.Value);
            field.EmitSignal(LineEdit.SignalName.TextChanged, field.Text);
            if (field.GetParent() is SpinBox)
            {
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = false });
            }
        }
        else return "Unknown command. Nothing changed.";
        await Settle();
        return $"{verb} \"{target.Text}\".";
    }

    private async Task MouseClick(PlayScreen.Entry target)
    {
        var point = target.Rect.GetCenter();
        // Input.ParseInputEvent also updates Godot's global mouse-button state. PopupMenu
        // uses that state to distinguish opening a picker from selecting an item.
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = 0, Pressed = false });
    }

    private static double Similarity(string label, string query)
    {
        var left = label.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var right = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return left.Intersect(right).Count() + (label.Contains(query, StringComparison.OrdinalIgnoreCase) ? 10 : 0);
    }

    internal async Task<(string png, string txt)> Capture(string result)
    {
        await Settle();
        var stem = Path.Combine(output, (++sequence).ToString("D4"));
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        if (image.SavePng(stem + ".png") != Error.Ok) throw new IOException("Cannot save the screen.");
        File.WriteAllText(stem + ".txt", screen.Describe(result));
        CopyInputLog();
        return (stem + ".png", stem + ".txt");
    }

    private void CopyInputLog()
    {
        if (File.Exists(Shell.AutosavePath)) File.Copy(Shell.AutosavePath, Path.Combine(output, "input-log.json"), true);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) { CopyInputLog(); GetTree().Quit(); }
    }
}
