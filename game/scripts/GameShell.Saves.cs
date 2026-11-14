using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal string? AutosavePathOverride { get; set; }
    internal string AutosavePath => AutosavePathOverride ?? ProjectSettings.GlobalizePath("user://autosave.json");
    internal Button SaveButton { get; private set; } = null!;
    internal Button ContinueSaveButton { get; private set; } = null!;
    internal Button LoadSaveButton { get; private set; } = null!;
    internal FileDialog SaveFilePicker { get; private set; } = null!;
    private string? saveError;

    private void BuildSaveControls()
    {
        var row = new HBoxContainer { Name = "SaveRow" };
        var board = GetNode<VBoxContainer>("Margin/Layout/WorkArea/BoardPanel/BoardLayout");
        board.AddChild(row); board.MoveChild(row, 2);
        ContinueSaveButton = DeskUi.Button("Continue saved game", () => LoadSavedGame(AutosavePath));
        ContinueSaveButton.TooltipText = "Resume the latest autosave, including decisions already made this morning.";
        LoadSaveButton = DeskUi.Button("Load…", () => SaveFilePicker.PopupCenteredRatio(.75f));
        SaveButton = DeskUi.Button("Save", () =>
        {
            if (session.View is null) return;
            TryAutosave();
            RefreshDisplay(saveError is null ? $"Saved to {AutosavePath}." : "Your game is still open; the previous save is retained.");
        });
        row.AddChild(ContinueSaveButton); row.AddChild(LoadSaveButton); row.AddChild(SaveButton);
        SaveFilePicker = new FileDialog
        {
            Name = "LoadSavedGame", Title = "Load a saved game", Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile, Filters = ["*.json ; Game saves and input logs"],
            CurrentDir = ProjectSettings.GlobalizePath("user://")
        };
        AddChild(SaveFilePicker);
        SaveFilePicker.FileSelected += LoadSavedGame;
    }

    private void RefreshSaveControls()
    {
        SaveButton.Disabled = session.View is null;
        SaveButton.TooltipText = $"Save current decisions to {AutosavePath}. Autosaves also run each morning.";
        ContinueSaveButton.Disabled = !File.Exists(AutosavePath);
    }

    private void TryAutosave()
    {
        try { GameSaveStore.Write(AutosavePath, session.CreateSave()); saveError = null; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { saveError = error.Message; }
    }

    private bool AdvanceAndAutosave()
    {
        if (!session.RequestNextDay()) return false;
        TryAutosave();
        return true;
    }

    internal void LoadSavedGame(string path)
    {
        FinishDayPlayback(false);
        // File input is a UI boundary. Malformed or incompatible saves must never destroy the open game.
        try
        {
            var save = GameSaveStore.Read(path);
            var index = -1;
            ScenarioBoardData? data = null;
            for (var i = 0; i < scenarios.Count; i++)
            {
                var candidate = ScenarioBoardData.Load(scenarios[i].ScenarioPath);
                if (candidate.Id == save.Scenario) { index = i; data = candidate; break; }
            }
            if (index < 0) throw new InvalidDataException($"Scenario '{save.Scenario}' is not installed.");
            var replayPaths = FindReplays(scenarios[index].ScenarioPath);
            session.ResumeScenario(scenarios[index].ScenarioPath, save);
            saveError = null;
            ResetDeskSelection();
            Map.ShowScenario(data);
            GetNode<OptionButton>("%ScenarioPicker").Select(index);
            SetReplays(replayPaths);
            GetNode<TabContainer>("%DeskTabs").CurrentTab = TodayTab;
            RefreshDisplay(DeveloperTools ? $"Resumed {Path.GetFileName(path)}. Your saved decisions are applied; future days are yours to play." : "Your saved deployment is ready. Pick up where you left off.");
            ShowGameSurface();
        }
        catch (Exception error)
        {
            var message = DeveloperTools ? $"Cannot load save: {error.Message}" : "We couldn't continue your saved deployment. Your current game is unchanged.";
            RefreshDisplay(message);
            if (AtFrontDoor) frontMessage.Text = message;
        }
    }

    private static string[] FindReplays(string scenarioPath)
    {
        var directory = Path.Combine(Path.GetDirectoryName(scenarioPath)!, "golden");
        return Directory.Exists(directory) ? Directory.GetFiles(directory, "approach*.json").Order(StringComparer.Ordinal).ToArray() : [];
    }

    private void SetReplays(string[] paths)
    {
        replays = paths;
        var picker = GetNode<OptionButton>("%ReplayPicker"); picker.Clear();
        foreach (var replay in paths) picker.AddItem(Path.GetFileNameWithoutExtension(replay).Replace('-', ' '));
    }
}
