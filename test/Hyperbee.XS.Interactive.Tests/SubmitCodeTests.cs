using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;

namespace Hyperbee.XS.Interactive.Tests;

[TestClass]
public class SubmitCodeTests
{
    private Kernel _kernel;

    [TestInitialize]
    public async Task InitializeKernel()
    {
        _kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseWho()
                .UseValueSharing()
        };

        await new XsKernelExtension().OnLoadAsync( _kernel );
    }

    [TestCleanup]
    public void CleanUpKernel()
    {
        _kernel?.Dispose();
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldSwitchToXs()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync( "#!xs" );

        Assert.AreEqual( 1, events.Count );
        Assert.AreEqual( typeof( SubmitCode ), events[0].Command.GetType() );
        Assert.AreEqual( "#!xs", ((SubmitCode) events[0].Command).Code );
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldRunXs()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        var script = "#\u0021xs\n\nvar number = 123;\nnumber;\n";

        await _kernel.SubmitCodeAsync( script );

        AssertSuccess( events );
        Assert.IsTrue( events.OfType<DisplayedValueProduced>().Any( x => (x.Value as string) == "123" ) );
    }
#if !NET9_0
    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldAddPackage()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        var script = "#\u0021xs\n\npackage Humanizer.Core;\nusing Humanizer;\n\nvar x = 1+5;\nvar y = x.ToWords();\ndisplay(y);\n";

        await _kernel.SubmitCodeAsync( script );

        AssertSuccess( events );
        Assert.IsTrue( events.OfType<DisplayedValueProduced>().Any( x => (x.Value as string) == "six" ) );
    }
#endif

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldKeepVariables()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            "#\u0021xs\n\nvar x = 40;\n#\u0021whos\n" );

        var displayResult = GetDisplayResult( events );

        Assert.HasCount( 1, displayResult );
        Assert.AreEqual( "x:40", displayResult[0] );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            "#\u0021xs\nx++;\nx++;\n\n#\u0021whos\n" );

        displayResult = GetDisplayResult( events );

        Assert.HasCount( 1, displayResult );
        Assert.AreEqual( "x:42", displayResult[0] );
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldRedefineVariable()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            "#\u0021xs\n\nvar x = 123;\nvar y = \"hello\";\n#\u0021whos\n" );

        var displayResult = GetDisplayResult( events );

        Assert.HasCount( 2, displayResult );
        Assert.AreEqual( "y:\"hello\"", displayResult[1] );
        Assert.AreEqual( "x:123", displayResult[0] );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            "#\u0021xs\n\nvar x = \"world\";\n#\u0021whos\n" );

        displayResult = GetDisplayResult( events );

        Assert.HasCount( 2, displayResult );
        Assert.AreEqual( "y:\"hello\"", displayResult[1] );
        Assert.AreEqual( "x:\"world\"", displayResult[0] );
        events.Clear();

    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldShareVariablesWithKernels()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            "#\u0021csharp\n\nvar simple = \"test\";\n" );

        events.Clear();

        await _kernel.SubmitCodeAsync(
            "#\u0021xs\n\n#\u0021share --from csharp --name \"simple\" --as \"zSimple\"\n#\u0021whos\n" );

        var displayResult = GetDisplayResult( events );

        Assert.HasCount( 2, displayResult );
        Assert.AreEqual( "simple:\"test\"", displayResult[0] );
        Assert.AreEqual( "zSimple:\"test\"", displayResult[1] );
        events.Clear();

    }

    private static string[] GetDisplayResult( SubscribedList<KernelEvent> events )
    {
        return [.. events
            .OfType<ValueProduced>()
            .Select( x => $"{x.Name}:{x.FormattedValue.Value}" )
        ];
    }

    private static void AssertSuccess( SubscribedList<KernelEvent> events )
    {
        var failures = events.OfType<CommandFailed>().ToArray();

        if ( failures.Length > 0 )
            Assert.Fail( string.Join( '\n', failures.Select( x => x.Message ) ) );
        else
            Assert.IsTrue( events.OfType<CommandSucceeded>().Any() );
    }

}
