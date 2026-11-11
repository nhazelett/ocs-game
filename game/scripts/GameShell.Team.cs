using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal string? TeamSettingsPathOverride { get; set; }
    private string TeamSettingsPath => TeamSettingsPathOverride ?? ProjectSettings.GlobalizePath("user://team.cfg");
    internal PanelContainer TeamPanel { get; private set; } = null!;
    internal Button TeamCustomizeButton { get; private set; } = null!;
    internal Button TeamApplyButton { get; private set; } = null!;
    internal Button TeamBackButton { get; private set; } = null!;
    internal Dictionary<string, ProgressBar> TeamBars { get; } = [];
    internal Dictionary<string, Button> TeamButtons { get; } = [];
    internal Dictionary<string, LineEdit> TeamNameInputs { get; } = [];
    internal Dictionary<string, OptionButton> TeamFaceInputs { get; } = [];
    internal Dictionary<string, PersonPortrait> TeamFaces { get; } = [];
    internal Dictionary<string, OptionButton> WorkAssignees { get; } = [];
    internal Dictionary<string, Button> WorkAssignButtons { get; } = [];
    private readonly Dictionary<string, TeamPick> teamPicks = [];
    private HBoxContainer teamStrip = null!;
    private VBoxContainer teamContents = null!;
    private Label teamMessage = null!;
    private bool editingTeam;
    private string? focusedTeamMember;
    private string[] teamPortraits = [];
    private const string TeamSetupText = "Choose your team's names and faces before leaving. They are remembered for the next new deployment; saved games keep their own team.";

    private void BuildTeamDesk()
    {
        var shop = GetNode<VBoxContainer>("Margin/Layout/TopStrip/Margin/Row/Shop");
        shop.CustomMinimumSize = Vector2.Zero;
        teamStrip = new HBoxContainer { Name = "NamedTeam" };
        teamStrip.AddThemeConstantOverride("separation", 10);
        shop.AddChild(teamStrip);
        TeamPanel = new PanelContainer { Name = "Team", ZIndex = 35, Visible = false };
        AddChild(TeamPanel); TeamPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = new MarginContainer(); TeamPanel.AddChild(margin);
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 30);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 14); margin.AddChild(column);
        var heading = DeskUi.Label("Your contracting team"); heading.AddThemeFontSizeOverride("font_size", 30); column.AddChild(heading);
        teamMessage = DeskUi.Label(); column.AddChild(teamMessage);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        teamContents = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        teamContents.AddThemeConstantOverride("separation", 18); scroll.AddChild(teamContents);
        var footer = new HBoxContainer(); column.AddChild(footer);
        TeamBackButton = DeskUi.Button("Back to the desk", CloseTeam); footer.AddChild(TeamBackButton);
        TeamApplyButton = DeskUi.Button("Use these names and faces", ApplyTeamChoices);
        TeamApplyButton.ThemeTypeVariation = "TabletopPrimary"; footer.AddChild(TeamApplyButton);
        teamPortraits = Directory.GetFiles(Path.Combine(GamePaths.AssetRoot, "portraits"), "*.svg")
            .Select(Path.GetFileNameWithoutExtension).Where(x => x is not null && x != "fallback")
            .Cast<string>().Order(StringComparer.Ordinal).Prepend("").ToArray();
    }

    private void PrepareTeamChoices()
    {
        teamPicks.Clear();
        using var config = new ConfigFile();
        config.Load(TeamSettingsPath);
        var usedDefaults = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in session.PrepTeam)
        {
            var defaultFace = member.Portrait;
            if (string.IsNullOrEmpty(defaultFace) || !teamPortraits.Contains(defaultFace))
                defaultFace = teamPortraits.FirstOrDefault(face => face.Length > 0 && !usedDefaults.Contains(face)) ?? "";
            usedDefaults.Add(defaultFace);
            var section = session.PrepScenarioId + "." + member.Id;
            var nameValue = config.GetValue(section, "name", member.Name);
            var faceValue = config.GetValue(section, "portrait", defaultFace);
            var name = nameValue.VariantType == Variant.Type.String ? nameValue.AsString().Trim() : member.Name;
            var face = faceValue.VariantType == Variant.Type.String ? faceValue.AsString() : defaultFace;
            if (name.Length is 0 or > 40) name = member.Name;
            if (!teamPortraits.Contains(face)) face = defaultFace;
            teamPicks[member.Id] = new TeamPick { Id = member.Id, Name = name, Portrait = face };
        }
    }

    private void AddTeamPreparation(VBoxContainer parent)
    {
        PrepareTeamChoices();
        TeamCustomizeButton = DeskUi.Button("Your team · choose names and faces", () =>
        {
            editingTeam = true; RenderTeamPanel(); TeamPanel.Show();
            TeamNameInputs.Values.FirstOrDefault()?.GrabFocus();
        });
        TeamCustomizeButton.Visible = session.PrepTeam.Count > 0;
        parent.AddChild(TeamCustomizeButton);
    }

    private void CloseTeam()
    {
        TeamPanel.Hide();
        if (editingTeam) TeamCustomizeButton.GrabFocus();
        else if (focusedTeamMember is not null && TeamButtons.TryGetValue(focusedTeamMember, out var button)) button.GrabFocus();
    }

    private void ApplyTeamChoices()
    {
        if (TeamNameInputs.Values.Any(input => input.Text.Trim().Length is 0 or > 40))
        {
            teamMessage.Text = "Give each person a name of 1 to 40 characters.";
            TeamNameInputs.Values.First(input => input.Text.Trim().Length is 0 or > 40).GrabFocus();
            return;
        }
        using var config = new ConfigFile();
        config.Load(TeamSettingsPath);
        foreach (var member in session.PrepTeam)
        {
            var picker = TeamFaceInputs[member.Id];
            var pick = new TeamPick { Id = member.Id, Name = TeamNameInputs[member.Id].Text.Trim(),
                Portrait = picker.GetItemMetadata(picker.Selected).AsString() };
            teamPicks[member.Id] = pick;
            var section = session.PrepScenarioId + "." + member.Id;
            config.SetValue(section, "name", pick.Name!); config.SetValue(section, "portrait", pick.Portrait!);
        }
        if (config.Save(TeamSettingsPath) != Error.Ok)
        {
            teamMessage.Text = "These choices work for this deployment, but could not be remembered on this computer.";
            return;
        }
        CloseTeam();
    }

    private string TeamName(TeamMemberView member)
    {
        var chosen = member.Role == "you" ? session.CreateSave().Team?.FirstOrDefault(p => p.Id == member.Id)?.Name : null;
        return member.Role == "you" ? chosen is { Length: > 0 } ? "You · " + chosen : "You"
            : string.IsNullOrEmpty(member.Rank) ? member.Name : member.Rank + " " + member.Name;
    }

    private static PersonView TeamPerson(string id, string name, string? rank, string? portrait)
        => new(id, name, rank ?? "", "Contracting shop", "team", portrait, new Dictionary<string, string>());

    private void RefreshTeam()
    {
        if (teamStrip is null) return;
        DeskUi.Clear(teamStrip); TeamBars.Clear(); TeamButtons.Clear();
        GetNode<Label>("%ShopHoursLabel").Hide(); GetNode<ProgressBar>("%ShopHoursGauge").Hide();
        foreach (var member in session.View?.Team ?? [])
        {
            var card = new VBoxContainer { CustomMinimumSize = new Vector2(160, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            teamStrip.AddChild(card);
            var button = DeskUi.Button(TeamName(member), () =>
            {
                editingTeam = false; focusedTeamMember = member.Id;
                RenderTeamPanel(); TeamPanel.Show(); TeamBackButton.GrabFocus();
            });
            button.AutowrapMode = TextServer.AutowrapMode.Off;
            button.ClipText = true; button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            button.CustomMinimumSize = new Vector2(180, 44);
            button.TooltipText = TeamName(member) + ". " + member.RoleLabel + ". " + member.Pace + " Open to hand out work.";
            card.AddChild(button); TeamButtons[member.Id] = button;
            card.AddChild(DeskUi.Label($"{member.HoursSpokenForToday} of {member.HoursPerDay} h assigned"));
            var bar = new ProgressBar { MaxValue = Math.Max(1, member.HoursPerDay), Value = member.HoursSpokenForToday,
                ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12), MouseFilter = MouseFilterEnum.Ignore,
                TooltipText = $"{member.FreeHoursToday} hours free today; {member.HoursQueued} hours of unfinished work." };
            card.AddChild(bar); TeamBars[member.Id] = bar;
        }
        if (TeamPanel.Visible && !editingTeam) RenderTeamPanel();
    }

    private void RenderTeamPanel()
    {
        DeskUi.Clear(teamContents);
        TeamNameInputs.Clear(); TeamFaceInputs.Clear(); TeamFaces.Clear();
        WorkAssignees.Clear(); WorkAssignButtons.Clear();
        TeamApplyButton.Visible = editingTeam;
        TeamBackButton.Text = editingTeam ? "Back to preparations" : "Back to the desk";
        teamMessage.Text = editingTeam
            ? TeamSetupText
            : "New work stays with you until you hand it over. Select a name below a task, then give them the work. Free hours go to walk-in small buys.";
        if (editingTeam)
        {
            foreach (var member in session.PrepTeam)
            {
                var pick = teamPicks[member.Id];
                var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 18); teamContents.AddChild(row);
                var portrait = new PersonPortrait(); portrait.SetPerson(TeamPerson(member.Id, pick.Name!, member.Rank, pick.Portrait)); row.AddChild(portrait);
                TeamFaces[member.Id] = portrait;
                var details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(details);
                details.AddChild(DeskUi.Label(member.Role switch { "you" => "You", "experienced" => "Experienced contracting officer", _ => "Junior contracting officer" }));
                var name = new LineEdit { Text = pick.Name!, MaxLength = 40, PlaceholderText = "Name",
                    TooltipText = "Name for " + member.Name, CustomMinimumSize = new Vector2(0, 44) };
                details.AddChild(name); TeamNameInputs[member.Id] = name;
                name.TextChanged += _ => { if (TeamNameInputs.Count == session.PrepTeam.Count && TeamNameInputs.Values.All(input => input.Text.Trim().Length is > 0 and <= 40)) teamMessage.Text = TeamSetupText; };
                var face = new OptionButton { CustomMinimumSize = new Vector2(0, 44), TooltipText = "Face for " + member.Name };
                for (var i = 0; i < teamPortraits.Length; i++)
                {
                    face.AddItem(i == 0 ? "No portrait" : "Face " + i); face.SetItemMetadata(i, teamPortraits[i]);
                    if (teamPortraits[i] == pick.Portrait) face.Select(i);
                }
                face.ItemSelected += index => portrait.SetPerson(TeamPerson(member.Id, name.Text, member.Rank, teamPortraits[(int)index]));
                details.AddChild(face); TeamFaceInputs[member.Id] = face;
            }
            return;
        }
        if (session.View is not { } view) return;
        foreach (var member in view.Team)
        {
            var card = new VBoxContainer(); card.AddThemeConstantOverride("separation", 8); teamContents.AddChild(card);
            var header = new HBoxContainer(); card.AddChild(header);
            var portrait = new PersonPortrait(); portrait.SetPerson(TeamPerson(member.Id, TeamName(member), member.Rank, member.Portrait)); header.AddChild(portrait);
            var role = member.Role == "you" ? "" : " · " + member.RoleLabel;
            var text = DeskUi.Label($"{TeamName(member)}{role}\n{member.Pace}\n{member.FreeHoursToday} hours free today · {member.HoursQueued} hours of unfinished work");
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(text);
            if (member.Work.Count == 0) card.AddChild(DeskUi.Label("No unfinished tasks."));
            foreach (var work in member.Work)
            {
                card.AddChild(DeskUi.Label(work.Label.TrimEnd('.') + $" · {work.HoursLeft} hours left" +
                    (work.Ready ? "" : $" · waiting until day {work.WaitsUntilDay}")));
                var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12); card.AddChild(row);
                var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44) };
                var target = member.Id;
                foreach (var candidate in view.Team)
                {
                    picker.AddItem($"{TeamName(candidate)} · {view.HoursIfAssigned(work.WorkId, candidate.Id)} hours at their pace");
                    picker.SetItemMetadata(picker.ItemCount - 1, candidate.Id);
                    if (candidate.Id == member.Id) picker.Select(picker.ItemCount - 1);
                }
                row.AddChild(picker); WorkAssignees[work.WorkId] = picker;
                var give = DeskUi.Button("Give them this task", () =>
                    SendAction(new AssignAction { Day = session.View!.Day, Work = work.WorkId, To = target }));
                give.Disabled = true; row.AddChild(give); WorkAssignButtons[work.WorkId] = give;
                picker.ItemSelected += index =>
                {
                    target = picker.GetItemMetadata((int)index).AsString();
                    give.Disabled = view.Finished || target == member.Id;
                };
                picker.Disabled = view.Finished;
            }
        }
    }
}
