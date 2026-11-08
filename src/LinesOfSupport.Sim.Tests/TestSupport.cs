using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

internal static class Repo
{
    private static readonly Lazy<string> RootLazy = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios", "rules")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repository root (with scenarios/rules) not found above the test binaries");
    });

    public static string Root => RootLazy.Value;
    public static string Scenario(string id) => Path.Combine(Root, "scenarios", id);
    public static ScenarioBundle Load(string id) => ScenarioBundle.Load(Scenario(id));
}
