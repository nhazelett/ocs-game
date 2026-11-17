using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class RequestBuyFitSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial"); shell.DeveloperToggle.ButtonPressed = false;
            Open("tutWater"); Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc"); await Frames();
            var hash = shell.Session.StateHash;
            FitWater(); await Capture("water-before");
            Require(shell.Session.StateHash == hash, "Reading and fitting the form does not act.");
            var candidate = shell.PendingAward!;
            var expected = LinesOfSupport.Sim.Replay.Resume(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")), shell.Session.CreateSave());
            Require(expected.Apply(candidate).Accepted, "The independent Sim accepts the exact water buy.");
            var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.Session.StateHash == expected.StateHash() && shell.RequestWorkspace.Visible && shell.SelectedCard == "tutWater", "One exact buy refreshes the same open request page.");
            var standing = shell.RequestContents.GetChildren().OfType<Label>().Single(l => l.HasMeta("request_standing"));
            Require(standing.Text == shell.Session.View!.WhereThingsStand().Single(s => s.CardId == "tutWater").Sentence
                && shell.RequestScroll.GetGlobalRect().Encloses(standing.GetGlobalRect()), "The updated standing is exact and visible immediately after buying.");
            Require(shell.PendingAward is null && !shell.AwardSummaryLabel.Visible && shell.RequestBuyWarnings.Values.All(l => !l.Visible), "The committed quote and its warnings clear without closing the page.");
            await Capture("water-after");
            Scenario(shell, "tutorial"); shell.DeveloperToggle.ButtonPressed = false; Step(shell, 3); Open("tutForklift");
            Require(!shell.RequestPartLabels.ContainsKey("operator"), "Unknown parts stay hidden.");
            shell.RequirementAskButtons["dunn"].EmitSignal(Button.SignalName.Pressed); Step(shell); Open("tutForklift");
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa");
            Require(shell.AwardAddButtons.ContainsKey("operator") && shell.RequestBuyWarnings["operator"].Visible, "A learned-part quote warning also marks that known part on the request.");
            shell.RequirementAddButtons["operator"].EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.AwardPreview!.SoftGaps.Any(s => s.StartsWith("Harbor Equipment")) && shell.RequestBuyWarnings["operator"].Visible
                && shell.RequestPartLabels["operator"].GetMeta("buy_warning").AsBool(), "A supplier's omitted requested part is marked beside the exact Sim quote.");
            shell.RequestScroll.EnsureControlVisible(shell.RequestBuyWarnings["operator"]);
            var gaps = PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Single(l => l.Text == string.Join("\n\n", shell.AwardPreview!.SoftGaps));
            shell.AwardScroll.ScrollVertical += (int)(gaps.GlobalPosition.Y - shell.AwardScroll.GlobalPosition.Y); await Capture("omitted-operator");
            Pick(shell.InstrumentInput, ""); Pick(shell.VendorInput, "coastalRentals"); Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            Pick(shell.CorInput!, shell.Session.View!.CorChoices("mhe")[0].Id); await Frames();
            Require(!shell.RequestBuyWarnings["operator"].Visible, "Changing to a supplier that lists the part clears its stale marker: " + shell.RequestBuyWarnings["operator"].Text + " on=" + shell.Session.View!.RequirementEditor("tutForklift")!.Lines.Single(l => l.Id == "operator").On + " vendor=" + shell.PendingAward?.Vendor + " add=" + shell.AwardAddButtons.ContainsKey("operator"));
            shell.AwardScroll.ScrollVertical = 0; await Frames();
            var quote = shell.PendingAward!; var exact = shell.Session.View!.Preview(quote);
            Require(shell.AwardPreview!.TotalCents == exact.TotalCents && shell.AwardSummaryLabel.Text == shell.Session.View.AwardSummary(quote)!.Sentence, "The compact written form keeps the complete exact quote.");
            await Capture("forklifts-top");
            var pure = shell.Session.StateHash; var pinned = shell.AwardButton.GetGlobalRect();
            shell.AwardScroll.ScrollVertical = (int)(shell.AwardScroll.Size.Y * .75f); await Frames();
            Require(shell.PriorityInputs.Values.All(InForm) && InForm(shell.CorInput!) && InForm(shell.CorInput!.GetParent().GetChild<Control>(0)), "One normal scroll reaches pick two and the contract monitor together.");
            Require(shell.AwardButton.GetGlobalRect() == pinned && shell.AwardSummaryLabel.Text == shell.Session.View.AwardSummary(quote)!.Sentence, "Scrolling leaves the action and entire summary fixed.");
            Require(shell.Session.StateHash == pure, "Scrolling and warning marks are read only.");
            await Capture("forklifts-one-scroll");
            Scenario(shell, "spoke-b"); shell.DeveloperToggle.ButtonPressed = false; Open("req0007");
            Pick(shell.VendorInput, "vendorA"); Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            shell.AwardScroll.ScrollVertical = 0; await Capture("spoke-b-power");
            shell.QueueFree(); shell = null; await Frames();
            GD.Print("REQUEST_BUY_FIT_SMOKE_PASS: water fields without scrolling, exact visible standing after one buy, known-only warning marks and clearing, forklift priorities/COR within one scroll, pinned full summary and Spoke B."); GetTree().Quit();

            bool InForm(Control control) => control.IsVisibleInTree() && shell!.AwardScroll.GetGlobalRect().Encloses(control.GetGlobalRect());
            void Open(string card) => shell!.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
            void FitWater()
            {
                Require(shell!.AwardScroll.ScrollVertical == 0, "The water form starts at the top.");
                foreach (var control in new Control[] { shell.InstrumentInput, shell.QuantityInput, shell.StartInput, shell.DurationInput, shell.VendorInput })
                    Require(InForm(control) && InForm(control.GetParent().GetChild<Control>(0)), "Every required water field and caption is visible without scrolling: " + control.Name);
                Require(shell.RequestBuyColumn.GetGlobalRect().Encloses(shell.AwardSummaryLabel.GetGlobalRect()) && shell.RequestBuyColumn.GetGlobalRect().Encloses(shell.AwardButton.GetGlobalRect()), "The complete water summary and action remain in view.");
                Require(shell.RequestColumn.Size.X >= shell.RequestWorkspace.Size.X / 3, "The request keeps at least a third of the page.");
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                await Frames(); var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots"); if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native buy-layout capture saved.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("REQUEST_BUY_FIT_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
