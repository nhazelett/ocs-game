using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// DESK-217's recorded third play, exercised by the existing BPA CI scene.
internal static class SmallScreenSmokeChecks
{
    internal static async Task Run(Node scene, GameShell shell)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var logs = Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-22-3");
        var log = InputLog.Load(Path.Combine(logs, "input-log-2.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 7);
        Load(expected); shell.GoToMarket("tutForklift"); await Frames();
        var links = PlayScreen.Descendants(shell.RequestDecisionRows!).OfType<Button>().Where(b => b.HasMeta("decision_contract")).ToArray();
        Require(links.Length > 0 && links.All(b => b.Text == "Open Coastal Rentals' contract page →"),
            "The exact forklift request uses Sim's possessive for Coastal Rentals.");
        var hash = shell.Session.StateHash;
        await Capture("supplier", shell.RequestScroll, links.Last());
        links.Last().EmitSignal(Button.SignalName.Pressed); await Frames();
        var arrangement = expected.Arrangements.Single(a => a.Id == links.Last().GetMeta("decision_contract").AsString());
        Require(shell.ContractView.Visible && shell.SelectedArrangement == arrangement.Id && shell.Session.StateHash == hash,
            "The possessive link still opens its exact Coastal Rentals contract without acting.");

        expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 12);
        Load(expected); await Frames();
        var issue = expected.View.PerformanceIssues.Single(i => i.CapabilityId == "food");
        shell.TodayPerformanceLinks[issue.Id].EmitSignal(Button.SignalName.Pressed); await Frames();
        var resource = shell.PerformanceChoices[(issue.Id, "resource")];
        var reason = resource.GetParent().GetChild<Label>(resource.GetIndex() + 1);
        Require(resource.Disabled && reason.Visible && reason.Text == expected.View.ResourceReason(issue.Id)
            && reason.Text == "No other supplier the shop knows offers food service at Hub.",
            "The dining-hall Re-source button is greyed with the authoritative reason beneath it.");
        var inbox = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
        await Capture("no-supplier", inbox, reason);
        hash = shell.Session.StateHash; resource.EmitSignal(Button.SignalName.Pressed);
        Require(!shell.RequestWorkspace.Visible && shell.Session.StateHash == hash, "Even a stale/programmatic disabled Re-source cannot open a buy.");
        shell.OpenContractView(issue.ArrangementId); await Frames();
        var contractResource = shell.ContractPerformanceChoices[(issue.Id, "resource")];
        var contractReason = contractResource.GetParent().GetChild<Label>(contractResource.GetIndex() + 1);
        Require(contractResource.Disabled && contractReason.Text == reason.Text, "The contract page offers the same greyed Re-source and reason.");
        contractResource.EmitSignal(Button.SignalName.Pressed);
        Require(!shell.RequestWorkspace.Visible && shell.Session.StateHash == hash, "The contract's unavailable Re-source cannot navigate either.");
        Require(shell.SendAction(new CureNoticeAction { Day = expected.Day, Issue = issue.Id, Assignee = "you" }).Accepted,
            "The reported dining-hall issue accepts a cure held by you.");
        var held = shell.ContractPerformanceChoices[(issue.Id, "cure")];
        var cost = held.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > held.GetIndex() && l.HasMeta("action_cost"));
        Require(cost.Text.StartsWith("You have this task, about ") && !cost.Text.Contains("You has"),
            "A task held by you uses have on the same cost line used for the team's hand-over.");

        log = InputLog.Load(Path.Combine(logs, "input-log-1.json"));
        expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 2);
        Load(expected); await Frames();
        shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.VendorsTab;
        Pick(shell.VendorCardPicker, "sanitation");
        shell.EstablishVendorButtons["sanitation"].EmitSignal(Button.SignalName.Pressed);
        Pick(shell.BpaCapabilityInput, "waste"); shell.BpaScopeInputs["removal"].ButtonPressed = true;
        var picker = shell.EstablishAssigneePicker!;
        hash = shell.Session.StateHash;
        Require(picker.Selected == "you" && picker.Choices.SequenceEqual(expected.View.AssigneeChoices(shell.PendingEstablish!)),
            "BPA setup starts with you and every exact Sim assignee forecast.");
        foreach (var member in expected.View.Team)
        {
            shell.EstablishAssigneePicker!.ChoiceButtons[member.Id].EmitSignal(Button.SignalName.Pressed);
            var action = shell.PendingEstablish!; var preview = expected.View.PreviewEstablish(action);
            Require(action.Assignee == member.Id && shell.EstablishPreview is { } shown
                && shown.HoursMinMilli == preview.HoursMinMilli && shown.HoursMaxMilli == preview.HoursMaxMilli
                && shown.EarliestStartMin == preview.EarliestStartMin && shown.EarliestStartMax == preview.EarliestStartMax
                && shown.HardStops.SequenceEqual(preview.HardStops) && shell.Session.StateHash == hash,
                "Each setup assignee changes only the exact proposed action and its authoritative preview.");
        }
        shell.EstablishAssigneePicker!.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
        var candidate = shell.PendingEstablish!;
        var footer = shell.EstablishButton.GetParent();
        Require(footer.GetChildren().OfType<Label>().Any(l => l.Text.Contains("Avery's time") && l.Text.Contains("Earliest result: day")),
            "The pinned setup cost names Avery and her authoritative completion day.");
        picker = shell.EstablishAssigneePicker!;
        var scroll = AncestorScroll(picker);
        await Capture("bpa-assignee", scroll, picker);
        Require(scroll.GetGlobalRect().Encloses(picker.GetGlobalRect())
            && scene.GetViewport().GetVisibleRect().Encloses(scroll.GetGlobalRect())
            && scene.GetViewport().GetVisibleRect().Encloses(picker.GetGlobalRect())
            && scene.GetViewport().GetVisibleRect().Encloses(shell.EstablishButton.GetGlobalRect()),
            "All three setup choices and the pinned commit control fit inside the real release viewport.");
        Require(expected.Apply(candidate).Accepted, "Independent Sim accepts Avery's exact BPA setup.");
        var submit = shell.EstablishButton; submit.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == expected.StateHash()
            && shell.Session.View!.Team.Single(m => m.Id == "avery").Work.Any(w => w.Label.Contains("BPA")),
            "Submitting assigns the actual setup work to Avery, with the same independent Sim hash.");
        Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(),
            "The setup assignee survives save/replay.");
        var after = shell.Session.StateHash; submit.EmitSignal(Button.SignalName.Pressed);
        picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == after && shell.PendingEstablish is null, "Closed setup controls cannot change or resubmit work.");
        shell.EstablishVendorButtons["sanitation"].EmitSignal(Button.SignalName.Pressed);
        Require(shell.EstablishAssigneePicker!.Selected == "you", "A fresh setup resets its assignee to you.");
        shell.VendorSetupBack.EmitSignal(Button.SignalName.Pressed);
        GD.Print("SMALL_SCREEN_SMOKE_PASS: third-play possessive and contract destination, both Re-source reasons and gates, You have, exact BPA assignee previews, assignment, stale controls and save/replay.");

        void Load(Session fixture)
        {
            var path = shell.AutosavePathOverride + ".small-screen";
            File.WriteAllText(path, fixture.ToSave().ToJson()); shell.LoadSavedGame(path); File.Delete(path);
            shell.DeveloperToggle.ButtonPressed = false;
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix, ScrollContainer scroll, Control focus)
        {
            await Frames(); scroll.EnsureControlVisible(focus); await Frames();
            Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()), $"The acceptance control fits its existing scroll: {suffix}, scroll={scroll.GetGlobalRect()}, control={focus.GetGlobalRect()}.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--small-screen-shots"); if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "The native acceptance screen is captured.");
        }
        static ScrollContainer AncestorScroll(Node node)
        {
            while (node is not ScrollContainer) node = node.GetParent();
            return (ScrollContainer)node;
        }
    }
}
