using System.Text.Json;
using Xunit;

namespace LinesOfSupport.Harness.Tests;

public sealed class HarnessTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "lines-of-support-tests-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateAcceptsDirectoryOrFileAndDisclosesFallback(bool useFile)
    {
        var scenario = Scenario();
        var result = Run("validate", useFile ? scenario : Path.GetDirectoryName(scenario)!);
        Assert.Equal(0, result.Code);
        Assert.Contains("Valid: test (30 days, 1 sites)", result.Output);
        Assert.Contains("fallback", result.Output);
        Assert.Empty(result.Error);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"\",\"days\":30,\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":0,\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":-1,\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":1.5,\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":\"30\",\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":2147483648,\"sites\":[{}]}")]
    [InlineData("{\"id\":\"test\",\"days\":30,\"sites\":[]}")]
    [InlineData("{\"id\":\"test\",\"days\":30,\"sites\":{}}")]
    public void ValidateRejectsBrokenScenario(string json)
    {
        var result = Run("validate", Scenario(json));
        Assert.Equal(1, result.Code);
        Assert.StartsWith("Error:", result.Error);
        Assert.DoesNotContain("Valid:", result.Output);
    }

    [Fact]
    public void MissingScenarioIsAFileError()
    {
        var result = Run("validate", Path.Combine(root, "missing"));
        Assert.Equal(1, result.Code);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public void RunCountsDaysAndDoesNotClaimToReplayInputLog()
    {
        var scenario = Scenario();
        var log = Write("input-log.json", "[{\"day\":1,\"action\":\"placeholder\"}]");
        var result = Run("run", scenario, "--seed", "42", "--policy", "textbook", "--log", log);
        Assert.Equal(0, result.Code);
        Assert.Contains("Days advanced: 30.", result.Output);
        Assert.Contains("seed: 42; policy: textbook", result.Output);
        Assert.Contains("NOT replayed", result.Output);
        Assert.Contains("M0 PLACEHOLDER", result.Output);
    }

    [Fact]
    public void RunIsRepeatableForTheSameArguments()
    {
        var scenario = Scenario();
        Assert.Equal(Run("run", scenario, "--seed", "-12"), Run("run", scenario, "--seed", "-12"));
    }

    [Theory]
    [InlineData("{", false)]
    [InlineData("[]", true)]
    public void RunChecksInputLogJsonAndExistence(string content, bool missing)
    {
        var scenario = Scenario();
        var log = missing ? Path.Combine(root, "missing.json") : Write("input-log.json", content);
        var result = Run("run", scenario, "--log", log);
        Assert.Equal(1, result.Code);
    }

    [Fact]
    public void BatchRunsTwoHundredSeedsForEveryRequestedPolicy()
    {
        var result = Run("batch", Scenario(), "--seeds", "200", "--policies", "do-nothing,greedy,textbook");
        Assert.Equal(0, result.Code);
        Assert.Contains("seeds: 0 through 199", result.Output);
        foreach (var policy in new[] { "do-nothing", "greedy", "textbook" })
            Assert.Contains($"| {policy} | 200 | 30 | 30.00 | 30 | not simulated |", result.Output);
        Assert.Contains("no mission outcomes are simulated", result.Output);
    }

    [Theory]
    [InlineData("run", "--seed", "NaN")]
    [InlineData("run", "--seed", "2147483648")]
    [InlineData("run", "--policy", "invented")]
    [InlineData("run", "--seeds", "200")]
    [InlineData("batch", "--seeds", "0")]
    [InlineData("batch", "--seeds", "-1")]
    [InlineData("batch", "--policies", "")]
    [InlineData("batch", "--policies", "greedy,greedy")]
    [InlineData("batch", "--policies", "greedy,")]
    [InlineData("validate", "--seed", "42")]
    public void BadArgumentsReturnUsageError(string verb, string option, string value)
    {
        var result = Run(verb, Scenario(), option, value);
        Assert.Equal(2, result.Code);
        Assert.Contains("Usage:", result.Error);
    }

    [Fact]
    public void RejectsUnknownCommandMissingValuesAndDuplicateOptions()
    {
        var scenario = Scenario();
        Assert.Equal(2, Run("invented", scenario).Code);
        Assert.Equal(2, Run("run", scenario, "--seed").Code);
        Assert.Equal(2, Run("run", scenario, "--seed", "--policy", "greedy").Code);
        Assert.Equal(2, Run("run", scenario, "--seed", "1", "--seed", "2").Code);
        Assert.Equal(2, Run().Code);
    }

    [Fact]
    public void HelpWorksWithoutAScenario()
    {
        Assert.Equal(0, Run("--help").Code);
        Assert.Equal(0, Run("help").Code);
    }

    [Fact]
    public void SchemaEnforcesConstraintsBeyondFallbackAndDoesNotLeaveGlobalState()
    {
        var scenario = Scenario();
        Write("scenarios/schema/scenario.schema.json", """
            {"$schema":"https://json-schema.org/draft/2020-12/schema",
             "type":"object","properties":{"days":{"maximum":10}}}
            """);
        var result = Run("validate", scenario);
        Assert.Equal(1, result.Code);
        Assert.Contains("failed JSON Schema validation", result.Error);
        var shortScenario = Scenario("""{"id":"test","days":5,"sites":[{}]}""");
        Assert.Equal(0, Run("validate", shortScenario).Code);
        Assert.Equal(0, Run("validate", shortScenario).Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SchemaResolvesSiblingReferencesByFileOrDeclaredId(bool declaredId)
    {
        var scenario = Scenario();
        var common = declaredId
            ? """{"$id":"https://example.test/common.json","$defs":{"days":{"maximum":10}}}"""
            : """{"$defs":{"days":{"maximum":10}}}""";
        Write("scenarios/schema/common.json", common);
        var reference = declaredId ? "https://example.test/common.json#/$defs/days" : "common.json#/$defs/days";
        Write("scenarios/schema/scenario.json", JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new { days = new Dictionary<string, string> { ["$ref"] = reference } }
        }));
        var result = Run("validate", scenario);
        Assert.Equal(1, result.Code);
        Assert.Contains("failed JSON Schema validation", result.Error);
    }

    [Fact]
    public void RulesAreSelectedByVersionFromTheSharedDirectory()
    {
        var scenario = Scenario("""{"id":"test","days":30,"sites":[{"id":"hub","island":"home"}],"islands":[{"id":"home"}],"rulesVersion":"test-only"}""");
        Write("scenarios/schema/scenario.json", "{\"type\":\"object\"}");
        Write("scenarios/schema/rules.json", "{\"type\":\"object\",\"required\":[\"rulesVersion\"]}");
        Assert.Contains("No shared rules file", Run("validate", scenario).Error);
        Write("scenarios/rules/rules-test-only.json", "{}");
        Assert.Contains("rules.json failed JSON Schema validation", Run("validate", scenario).Error);
        Write("scenarios/rules/rules-test-only.json", "{\"rulesVersion\":\"test-only\"}");
        var valid = Run("validate", scenario);
        Assert.Equal(0, valid.Code);
        Assert.Contains("repository JSON Schemas", valid.Output);
    }

    [Fact]
    public void ThemeIsOptionalButValidatedWhenPresent()
    {
        var scenario = Scenario();
        Write("scenarios/schema/scenario.json", "{\"type\":\"object\"}");
        Write("scenarios/schema/theme.json", "{\"required\":[\"title\"]}");
        Assert.Equal(0, Run("validate", scenario).Code);
        Write("scenarios/test/theme.json", "{}");
        Assert.Contains("theme.json failed JSON Schema validation", Run("validate", scenario).Error);
        Write("scenarios/test/theme.json", "{\"title\":\"test\"}");
        Assert.Equal(0, Run("validate", scenario).Code);
    }

    [Fact]
    public void MalformedCompanionIsRejectedEvenBeforeSchemasLand()
    {
        var scenario = Scenario();
        Write("scenarios/test/rules.json", "{");
        Assert.Equal(1, Run("validate", scenario).Code);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"type\":12}")]
    [InlineData("{\"$ref\":\"missing.json\"}")]
    public void BrokenSchemaIsAnErrorRatherThanFallbackSuccess(string schema)
    {
        var scenario = Scenario();
        Write("scenarios/schema/scenario.json", schema);
        var result = Run("validate", scenario);
        Assert.Equal(1, result.Code);
        Assert.StartsWith("Error:", result.Error);
        Assert.DoesNotContain("fallback", result.Output);
    }

    [Fact]
    public void RefusesFallbackWhenOnlyAnUnrecognizedSchemaNameExists()
    {
        var scenario = Scenario();
        Write("scenarios/schema/unrecognized.json", "{}");
        Assert.Contains("refusing fallback validation", Run("validate", scenario).Error);
    }

    private string Scenario(string json = """{"id":"test","days":30,"sites":[{"id":"hub"}]}""") =>
        Write("scenarios/test/scenario.json", json);

    private string Write(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static CommandResult Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = HarnessApp.Run(args, output, error);
        return new CommandResult(code, output.ToString(), error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private sealed record CommandResult(int Code, string Output, string Error);
}
