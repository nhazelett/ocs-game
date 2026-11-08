using System.Text.Json.Nodes;
using Xunit;

namespace LinesOfSupport.Harness.Tests;

public sealed class SchemaIntegrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "los-schema-integration-" + Guid.NewGuid().ToString("N"));
    private string ScenarioPath => Path.Combine(root, "scenarios", "_stub", "scenario.json");

    public SchemaIntegrationTests()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
        foreach (var file in Directory.GetFiles(fixtures, "*.json", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(root, Path.GetRelativePath(fixtures, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    [Fact]
    public void FablesActualStubPassesAllHarnessCommandsWithSharedRules()
    {
        foreach (var command in new[] { "validate", "run", "batch" })
            Assert.Equal(0, Run(command).Code);
        File.Delete(Path.Combine(root, "scenarios", "_stub", "theme.json"));
        Assert.Equal(0, Run("validate").Code);
    }

    [Theory]
    [InlineData("unknownRules", "No shared rules file")]
    [InlineData("siteIsland", "island: 'missing' not found")]
    [InlineData("linkIsland", "to island: 'missing' not found")]
    [InlineData("capability", "capability in rules")]
    [InlineData("cardSite", "card")]
    [InlineData("cardDates", "requested start before submission")]
    [InlineData("vendorIsland", "vendor")]
    [InlineData("eventWindow", "window ends before it starts")]
    [InlineData("eventAfterEnd", "window starts after the scenario ends")]
    [InlineData("followOn", "follow-on")]
    [InlineData("fixedSituation", "fixedSituations")]
    [InlineData("priority", "priorityOrder")]
    [InlineData("opensAfterEnd", "opens after the scenario ends")]
    [InlineData("duplicateId", "duplicate id")]
    public void BrokenReferencesAndWindowsAreRejected(string mutation, string expected)
    {
        var data = JsonNode.Parse(File.ReadAllText(ScenarioPath))!;
        switch (mutation)
        {
            case "unknownRules": data["rulesVersion"] = "1999-01"; break;
            case "siteIsland": data["sites"]![0]!["island"] = "missing"; break;
            case "linkIsland": data["transportLinks"]![0]!["to"] = "missing"; break;
            case "capability": data["sites"]![0]!["capabilities"]![0]!["id"] = "missing"; break;
            case "cardSite": data["requirementCards"]![0]!["site"] = "missing"; break;
            case "cardDates": data["requirementCards"]![0]!["submittedDay"] = 20; break;
            case "vendorIsland": data["vendors"]![0]!["island"] = "missing"; break;
            case "eventWindow": data["events"]![0]!["window"]!["latestDay"] = 0; break;
            case "eventAfterEnd": data["events"]![0]!["window"]!["earliestDay"] = 100; data["events"]![0]!["window"]!["latestDay"] = 101; break;
            case "followOn": data["events"]![0]!["followOns"] = new JsonArray("missing"); break;
            case "fixedSituation": data["fixedSituations"] = new JsonArray("missing"); break;
            case "priority": data["actors"]!["commander"]!["priorityOrder"] = new JsonArray("missing"); break;
            case "opensAfterEnd": data["sites"]![0]!["opensDay"] = 100; break;
            case "duplicateId": data["sites"]!.AsArray().Add(data["sites"]![0]!.DeepClone()); break;
        }
        File.WriteAllText(ScenarioPath, data.ToJsonString());
        var result = Run("validate");
        Assert.Equal(1, result.Code);
        Assert.Contains(expected, result.Error);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicateVersion")]
    [InlineData("badDate")]
    [InlineData("negativeThreshold")]
    public void SharedRulesMustExistBeUnambiguousAndMatchSchema(string mutation)
    {
        var path = Path.Combine(root, "scenarios", "rules", "rules-2025-10.json");
        var data = JsonNode.Parse(File.ReadAllText(path))!;
        switch (mutation)
        {
            case "missing": File.Delete(path); break;
            case "duplicateVersion": File.Copy(path, Path.Combine(Path.GetDirectoryName(path)!, "duplicate.json")); break;
            case "badDate": data["asOf"] = "not-a-date"; File.WriteAllText(path, data.ToJsonString()); break;
            case "negativeThreshold": data["thresholds"]!["microPurchaseCents"] = -1; File.WriteAllText(path, data.ToJsonString()); break;
        }
        Assert.Equal(1, Run("validate").Code);
    }

    private (int Code, string Error) Run(string verb)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        return (HarnessApp.Run([verb, ScenarioPath], output, error), error.ToString());
    }

    public void Dispose() => Directory.Delete(root, recursive: true);
}
