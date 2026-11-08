using Xunit;

namespace LinesOfSupport.Sim.Tests;

public sealed class ScaffoldTests
{
    // M0-08 only establishes the test runner. Fable replaces this with simulation tests in M0-05.
    [Fact]
    public void SimulationAssemblyLoads()
    {
        var assembly = System.Reflection.Assembly.Load("LinesOfSupport.Sim");
        Assert.Equal("LinesOfSupport.Sim", assembly.GetName().Name);
    }
}
