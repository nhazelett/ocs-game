using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class FrontDoorSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-front-" + Guid.NewGuid().ToString("N"));
        try
        {
            GetTree().Root.GuiEmbedSubwindows = true;
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            Require(shell.AtFrontDoor && !shell.DeveloperToggle.ButtonPressed && shell.Session.View is null, "Cold launch is title, developer tools off, no active session.");
            Require(shell.DeveloperToggle.IsVisibleInTree(), "Developer opt-in is in the title settings.");
            Require(shell.ContinueGameButton.Disabled, "Continue is unavailable without an autosave.");
            Require(!shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && !shell.LoadSaveButton.IsVisibleInTree(), "No file or scenario controls on the title.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            await Capture("-title");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.TutorialGameButton.GetIndex() < shell.SpokeBGameButton.GetIndex(), "Tutorial is the first deployment.");
            var descriptions = shell.GetNode("FrontDoor").FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.HasMeta("deployment")).ToArray();
            Require(descriptions.Single(l => l.GetMeta("deployment").AsString() == "tutorial").Text.EndsWith("16 days")
                && descriptions.Single(l => l.GetMeta("deployment").AsString() == "spoke-b").Text.EndsWith("30 days"), "Each deployment describes its purpose and authored length.");
            await Capture("-deployments");
            Require(!shell.SpokeBGameButton.Disabled, "Named Spoke B is offered without a file picker.");
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.Prep is not null && shell.Session.View is null, "New game opens Spoke B preparations before play.");
            Require(!shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && !shell.GetNode<Button>("%ReplayButton").IsVisibleInTree() && !shell.LoadSaveButton.IsVisibleInTree(), "Test controls remain hidden during normal play.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell); Step(shell);
            var hash = shell.Session.StateHash;
            Require(!shell.DeveloperToggle.IsVisibleInTree(), "The developer toggle is absent from player screens.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(shell.LeaveGameDialog.Visible && !shell.AtFrontDoor && shell.Session.StateHash == hash,
                "Menu asks before leaving and preserves the open deployment.");
            Require(shell.LeaveGameDialog.DialogText == "Leave this game?\n\nYour game is saved.", "The prompt says the deployment is saved.");
            Require(GameSaveStore.Read(shell.AutosavePath).SavedDay == shell.Session.View!.Day, "The prompt follows a successful current-morning save.");
            await Capture("-leave");
            Require(shell.LeaveGameDialog.Borderless && shell.LeaveGameDialog.Title.Length == 0,
                "The leave prompt has one heading inside its frame.");
            var promptCenter = (Vector2)shell.LeaveGameDialog.Position + (Vector2)shell.LeaveGameDialog.Size / 2;
            Require(promptCenter.DistanceTo(GetViewport().GetVisibleRect().GetCenter()) <= 1,
                "The leave prompt is centered in the whole player window.");
            shell.LeaveGameDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(!shell.AtFrontDoor && !shell.LeaveGameDialog.Visible && shell.Session.StateHash == hash, "Keep playing preserves the game.");
            shell.LeaveGameDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.AtFrontDoor && shell.Session.StateHash == hash, "A canceled leave cannot be confirmed later.");
            ReturnToMenu(shell);
            Require(!shell.ContinueGameButton.Disabled, "A deployment saved through normal play enables Continue.");
            shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.StateHash == hash && !shell.Session.IsReplay, "Continue resumes the exact deployment without future replay actions.");
            shell.DeveloperToggle.ButtonPressed = true;
            Require(shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && shell.LoadSaveButton.IsVisibleInTree(), "Explicit developer opt-in reveals file/test controls.");
            await Capture("-developer");
            shell.LoadSaveButton.EmitSignal(Button.SignalName.Pressed);
            shell.DeveloperToggle.ButtonPressed = false;
            Require(!shell.SaveFilePicker.Visible && !shell.LoadSaveButton.IsVisibleInTree(), "Turning developer tools off closes the file picker too.");
            ReturnToMenu(shell);
            File.WriteAllText(shell.AutosavePath, "{");
            shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.AtFrontDoor && shell.Session.StateHash == hash, "Malformed saved deployment is refused without losing the open session.");
            // A one-person shop still lists its real work, with no impossible transfer controls.
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            DeployWithoutPrep(shell);
            Pick(shell.PersonContactInput, "finance"); shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View!.Team.Count == 1 && shell.Session.View.Team.Single().Work.Count > 0, "Solo fixture has real unfinished work.");
            shell.TeamButtons.Values.Single().EmitSignal(Button.SignalName.Pressed);
            Require(shell.WorkAssignees.Count == 0 && shell.WorkAssignButtons.Count == 0, "Solo work offers no hand-over picker or button.");
            Require(!shell.TeamPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains("hand it over")), "Solo help does not tell the player to hand work over.");
            await Capture("-solo-team"); shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var today = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body");
            var inbox = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
            shell.BoardDetailsToggle.ButtonPressed = true;
            tabs.CurrentTab = GameShell.TodayTab; await Capture("-today"); today.ScrollVertical = 500;
            Require(today.ScrollVertical > 0, "Today fixture is actually scrolled.");
            tabs.CurrentTab = GameShell.InboxTab; await Capture("-inbox"); inbox.ScrollVertical = 500;
            Require(inbox.ScrollVertical > 0, "Inbox fixture is actually scrolled.");
            tabs.CurrentTab = GameShell.RequestsTab; tabs.CurrentTab = GameShell.TodayTab;
            Require(today.ScrollVertical == 0, "Opening Today starts at the top.");
            tabs.CurrentTab = GameShell.InboxTab;
            Require(inbox.ScrollVertical == 0, "Opening Inbox starts at the top.");
            inbox.ScrollVertical = 200; Step(shell); await Capture("-new-morning");
            Require(tabs.CurrentTab == GameShell.TodayTab && today.ScrollVertical == 0, "A new morning opens at the top of Today.");
            var facts = shell.Session.View.MapDay();
            var moves = facts.Runs.Count > 0 || facts.Shipments.Any(s => !s.CustomsHeld);
            Require(shell.MovementButton.Disabled == !moves && shell.MovementButton.Text == (moves ? "Show today's movement" : "Nothing moves today"), "Movement wording follows actual Sim movement.");
            var savedHash = shell.Session.StateHash;
            shell.AutosavePathOverride = temporary; // A directory cannot be atomically replaced by a save file.
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && !shell.LeaveGameDialog.Visible && shell.Session.StateHash == savedHash,
                "A failed save keeps the deployment open without promising it was saved.");
            shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json");
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b"); Replay(shell);
            for (var day = 0; day < 15 && shell.Session.View!.MapDay().Runs.Count == 0; day++) Step(shell);
            Require(shell.Session.View!.MapDay().Runs.Count > 0 && !shell.MovementButton.Disabled
                && shell.MovementButton.Text == "Show today's movement", "Actual crossing deliveries retain the replay-movement control.");
            shell.DeveloperToggle.ButtonPressed = false;
            await Capture("-movement");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("FRONT_DOOR_SMOKE_PASS: title settings, tutorial-first choices, leave/cancel/save/Continue, solo work, tab and morning scroll, movement wording and failed-save recovery."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"FRONT_DOOR_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
