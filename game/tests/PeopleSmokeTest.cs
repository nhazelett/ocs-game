using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PeopleSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-people-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            FinishOpening(shell);
            var view = shell.Session.View!; var hash = shell.Session.StateHash;
            var requests = view.Cards.Select(c => view.RequirementEditor(c.Id)).Where(r => r is not null).ToArray();
            Require(requests.Length > 0 && requests.All(r => r!.Person is not null && r.Message is not null), "Actual scenario supplies customer identities and messages.");
            Require(shell.RequestOpenButtons.Keys.Order().SequenceEqual(requests.Select(r => r!.CardId).Order()), "Exactly the submitted requirements arrive, including day-one requests.");
            static IEnumerable<Node> Descendants(Node root) => root.GetChildren().SelectMany(n => new[] { n }.Concat(Descendants(n)));
            var labels = string.Join("\n", Descendants(shell.CustomerMessages).OfType<Label>().Select(l => l.Text));
            foreach (var request in requests)
                Require(labels.Contains(request!.Person!.WithUnit) && labels.Contains(request.Message!) && !labels.Contains(request.CardId), "Player sees rank/name/unit and exact authored words, without card ids.");
            Require(Descendants(shell.CustomerMessages).OfType<PersonPortrait>().Count() == requests.Length, "Every customer has a neutral portrait with the data key retained as metadata.");
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.InboxTab;
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            var first = requests[0]!; shell.RequestOpenButtons[first.CardId].EmitSignal(Button.SignalName.Pressed);
            Require(shell.SelectedCard == first.CardId && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.RequestsTab && shell.Session.StateHash == hash, "Open navigates to the actual editable card without changing Sim.");
            Step(shell, 8);
            Require(shell.RequestOpenButtons.Keys.Order().SequenceEqual(shell.Session.View!.Cards.Where(c => shell.Session.View.RequirementEditor(c.Id) is not null).Select(c => c.Id).Order()), "Later mornings retain exactly the messages Sim has submitted.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("PEOPLE_SMOKE_PASS: day-one and later messages, real names/words, portraits, exact Open target and read-only navigation."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PEOPLE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
