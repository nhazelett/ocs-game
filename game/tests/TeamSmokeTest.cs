using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class TeamSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-team-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            GameShell NewShell()
            {
                var result = scene.Instantiate<GameShell>();
                result.TeamSettingsPathOverride = Path.Combine(temporary, "team.cfg");
                result.AutosavePathOverride = Path.Combine(temporary, "save.json");
                result.LookSettingsPathOverride = Path.Combine(temporary, "look.cfg");
                AddChild(result); return result;
            }
            var shell = NewShell();
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamCustomizeButton.Visible && shell.Session.View is null, "The authored team can be personalised before deploying.");
            shell.TeamCustomizeButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamNameInputs.Count == 3 && shell.TeamFaceInputs.Count == 3, "All three people have real name and portrait controls.");
            Require(shell.TeamFaces.Values.All(face => face.PortraitKey != "fallback")
                && shell.TeamFaces.Values.Select(face => face.PortraitKey).Distinct().Count() == 3,
                "A first deployment starts with three distinct people, not empty silhouettes.");
            var members = shell.Session.PrepTeam;
            var lead = members.Single(m => m.Role == "you");
            var experienced = members.Single(m => m.Role == "experienced");
            var junior = members.Single(m => m.Role == "junior");
            shell.TeamNameInputs[experienced.Id].Text = "";
            shell.TeamApplyButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamPanel.Visible && !File.Exists(shell.TeamSettingsPathOverride), "An empty name cannot be applied or remembered.");
            shell.TeamNameInputs[lead.Id].Text = "Morgan Vale";
            shell.TeamNameInputs[experienced.Id].Text = "Robin Hayes";
            shell.TeamNameInputs[junior.Id].Text = "Sam Reed";
            var face = shell.TeamFaceInputs[experienced.Id];
            face.Select(4); face.EmitSignal(OptionButton.SignalName.ItemSelected, 4);
            var faceKey = face.GetItemMetadata(4).AsString();
            Require(shell.TeamFaces[experienced.Id].PortraitKey == faceKey, "The selected face is rendered from the existing portrait set.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i=0;i<3;i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot+1]+"-"+suffix+".png")==Error.Ok, "Native team screenshot saved.");
            }
            shell.TeamApplyButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.TeamPanel.Visible, "Corrected names can be applied.");
            shell.TeamCustomizeButton.EmitSignal(Button.SignalName.Pressed);
            await Capture("names");
            shell.TeamApplyButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.TeamPanel.Visible && File.Exists(shell.TeamSettingsPathOverride), "Applying remembers the choices and returns to preparations.");
            DeployWithoutPrep(shell);
            var picks = shell.Session.CreateSave().Team!;
            Require(picks.Single(p=>p.Id==lead.Id).Name=="Morgan Vale" && picks.Single(p=>p.Id==experienced.Id).Portrait==faceKey,
                "The actual deployment records names and faces in its replayable input log.");
            var bundle=ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot,"tutorial"));
            var expected=Session.Create(bundle,1,[],picks);
            Require(shell.Session.StateHash==expected.StateHash(), "Personalising creates the exact Sim session with those prep inputs.");
            Require(shell.TeamBars.Count==3 && !shell.GetNode<Label>("%ShopHoursLabel").Visible
                && !shell.GetNode<ProgressBar>("%ShopHoursGauge").Visible, "Three named bars replace the anonymous aggregate gauge.");
            Require(shell.TeamButtons[lead.Id].Text=="You · Morgan Vale" && shell.TeamButtons[experienced.Id].Text.Contains("Robin Hayes"),
                "The chosen names are visible on the strip, including the player's name.");
            foreach(var member in shell.Session.View!.Team)
                Require(shell.TeamBars[member.Id].Value==member.HoursSpokenForToday && shell.TeamBars[member.Id].MaxValue==member.HoursPerDay,
                    "Every bar uses its own exact Sim hours.");
            shell.DeveloperToggle.ButtonPressed=false;
            await Capture("desk");
            Require(shell.GetNode<Button>("%NextDay").GetGlobalRect().End.X<=GetViewport().GetVisibleRect().End.X,
                "Three team bars leave End the day inside the viewport.");
            Require(shell.TeamBars.Values.Select(b=>b.GetGlobalRect().Position.Y).Distinct().Count()==1, "The three personal bars line up even when names differ in length.");
            Pick(shell.PersonContactInput,"finance"); shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            Require(expected.Apply(new FindFinanceAction{Day=expected.Day}).Accepted && shell.Session.StateHash==expected.StateHash(),
                "New work stays with you exactly as the Sim default specifies.");
            var work=shell.Session.View!.Team.Single(m=>m.Role=="you").Work.Single();
            var hash=shell.Session.StateHash;
            shell.TeamButtons[experienced.Id].EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamPanel.Visible && shell.Session.StateHash==hash, "Opening the team and inspecting paces is read only.");
            Require(shell.WorkAssignButtons[work.WorkId].Disabled, "Giving a task to its current owner is unavailable.");
            var picker=shell.WorkAssignees[work.WorkId];
            var choices = expected.View.AssigneeChoices(new AssignAction { Day = expected.Day, Work = work.WorkId });
            Require(picker.Choices.SequenceEqual(choices) && choices.Count == 3
                && choices.All(c => picker.ChoiceButtons[c.MemberId].Text == c.Sentence
                    && picker.ChoiceButtons[c.MemberId].Icon is not null && c.ResultDayMin is not null),
                "Every teammate has their face and exact Sim load, pace and completion forecast.");
            picker.ChoiceButtons[experienced.Id].EmitSignal(Button.SignalName.Pressed);
            Require(!shell.WorkAssignButtons[work.WorkId].Disabled && picker.Selected == experienced.Id
                && shell.Session.StateHash == hash, "Choosing a free teammate only selects; it does not apply work.");
            await Capture("work");
            shell.WorkAssignButtons[work.WorkId].EmitSignal(Button.SignalName.Pressed);
            Require(expected.Apply(new AssignAction{Day=expected.Day,Work=work.WorkId,To=experienced.Id}).Accepted
                && shell.Session.StateHash==expected.StateHash(), "Giving a task sends the exact AssignAction, with no Game-side work arithmetic.");
            Require(shell.Session.View.Team.Single(m=>m.Id==experienced.Id).Work.Single().WorkId==work.WorkId
                && shell.TeamBars[lead.Id].Value==0 && shell.TeamBars[experienced.Id].Value==expected.View.Team.Single(m=>m.Id==experienced.Id).HoursSpokenForToday,
                "The task and occupied hours move to the selected person.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            Step(shell);expected.AdvanceDay();
            Require(shell.Session.StateHash==expected.StateHash(), "The assigned person's pace governs the real next day.");
            var save=shell.Session.CreateSave(); var savedHash=shell.Session.StateHash;
            GameSaveStore.Write(shell.AutosavePath,save);
            shell.LoadSavedGame(shell.AutosavePath);
            Require(shell.Session.StateHash==savedHash && shell.TeamButtons[lead.Id].Text=="You · Morgan Vale"
                && shell.Session.View!.Team.Single(m=>m.Id==experienced.Id).Portrait==faceKey, "Save/resume keeps assignments, names, portraits and exact state.");
            shell.QueueFree();shell=null!;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            shell=NewShell();
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TeamCustomizeButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamNameInputs[experienced.Id].Text=="Robin Hayes"
                && shell.TeamFaceInputs[experienced.Id].GetItemMetadata(shell.TeamFaceInputs[experienced.Id].Selected).AsString()==faceKey,
                "A fresh shell and new game load the remembered names and faces.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            DeveloperWorkspace(shell);Scenario(shell,"spoke-b");
            Require(shell.TeamBars.Count==1 && shell.TeamButtons["you"].Text=="You"
                && !shell.TeamCustomizeButton.Visible,"An older single-person scenario keeps its actual roster and total hours.");
            Scenario(shell,"tutorial");
            shell.DeveloperToggle.ButtonPressed=false;
            while (shell.Session.View!.Day < 9) Step(shell);
            expected=LinesOfSupport.Sim.Replay.Resume(bundle,shell.Session.CreateSave());
            var order=new AwardAction { Day=9, Id="assignment-check", Instrument="purchaseOrder", Vendor="sanitation",
                Card="tutWaste", Quantity=3600, StartDay=13, DurationDays=8, FundingLine="om1" };
            Require(expected.Apply(order).Accepted && shell.SendAction(order).Accepted, "A real written order loads the player's queue.");
            Step(shell);expected.AdvanceDay();
            var busy=shell.Session.View!.Team.Single(m=>m.Role=="you");
            work=busy.Work.Single();
            Require(busy.HoursQueued>busy.HoursPerDay, "The fixture overloads the player beyond one day's hours.");
            shell.TeamButtons[busy.Id].EmitSignal(Button.SignalName.Pressed);
            picker=shell.WorkAssignees[work.WorkId];
            choices=expected.View.AssigneeChoices(new AssignAction { Day=expected.Day, Work=work.WorkId });
            var free=choices.Single(c=>c.MemberId==experienced.Id);
            var overloaded=choices.Single(c=>c.Current);
            Require(free.FreeHoursToday>0 && free.HoursQueued==0 && overloaded.HoursQueued>8
                && free.ResultDayMin<overloaded.ResultDayMin && overloaded.QueueClearsDay is not null,
                "Free and overloaded choices have genuinely different Sim forecasts.");
            Require(picker.ChoiceButtons.All(pair=>pair.Value.Text==choices.Single(c=>c.MemberId==pair.Key).Sentence),
                "Overloaded queues and current-owner timing are also Sim's exact sentences.");
            await Capture("overloaded");
            picker.ChoiceButtons[free.MemberId].EmitSignal(Button.SignalName.Pressed);
            Require(picker.ChoiceButtons.Values.Count(b=>b.ButtonPressed)==1 && picker.ChoiceButtons[free.MemberId].ButtonPressed,
                "Keyboard or pointer selection has a single visible choice.");
            await Capture("free");
            var stale=shell.WorkAssignButtons[work.WorkId];
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(expected.Apply(new AssignAction { Day=expected.Day, Work=work.WorkId, To=free.MemberId }).Accepted
                && shell.Session.StateHash==expected.StateHash(), "The overloaded handoff sends the exact selected Sim action.");
            var reassignedHash=shell.Session.StateHash;
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash==reassignedHash, "An old handoff control cannot apply a second action.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            while (expected.Day<=free.ResultDayMax) { Step(shell);expected.AdvanceDay(); }
            Require(shell.Session.StateHash==expected.StateHash() && !shell.Session.View!.Team.SelectMany(m=>m.Work).Any(w=>w.WorkId==work.WorkId),
                "The task finishes on the quoted day through the actual day loop.");
            shell.QueueFree();shell=null!;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GC.Collect();GC.WaitForPendingFinalizers();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("TEAM_SMOKE_PASS: names and portraits, three exact personal bars, pure inspection, task handoff, actual pace, save/resume, restart and legacy roster.");
            GetTree().Quit();
        }
        catch(Exception error){GD.PushError("TEAM_SMOKE_FAIL: "+error);GetTree().Quit(1);}
        finally{if(Directory.Exists(temporary))Directory.Delete(temporary,true);}
    }
}
