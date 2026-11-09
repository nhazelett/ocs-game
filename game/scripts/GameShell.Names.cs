using LinesOfSupport.Sim;
using System.Text.RegularExpressions;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    // Display labels only. Internal ids remain in action targets and control metadata.
    internal static string Words(object value) => Regex.Replace(value.ToString() ?? "", "(?<=[a-z])(?=[A-Z])", " ");

    private static string CapabilityName(SessionView view, string id)
        => view.Rules.Model.Capabilities.FirstOrDefault(c => c.Id == id) is { } capability ? capability.Label : "Support";

    private static string ScopeLineName(SessionView view, string id)
        => view.Rules.Model.Capabilities.SelectMany(c => c.Lines).FirstOrDefault(l => l.Id == id) is { } line
            ? line.Label ?? Words(line.Id) : "Support";
}
