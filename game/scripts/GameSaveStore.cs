using LinesOfSupport.Sim;

namespace LinesOfSupport.Game;

internal static class GameSaveStore
{
    internal static InputLog Read(string path)
    {
        var save = InputLog.Load(path);
        if (string.IsNullOrWhiteSpace(save.Scenario) || string.IsNullOrWhiteSpace(save.RulesVersion) ||
            save.Actions is null || save.Actions.Any(a => a is null) ||
            save.PrepChoices is null || save.PrepChoices.Any(p => p is null))
            throw new InvalidDataException("The save needs a scenario, rules version, actions and prep choices.");
        return save;
    }

    internal static void Write(string path, InputLog save)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        // Same-directory rename keeps the previous autosave intact if writing is interrupted.
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, save.ToJson());
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
