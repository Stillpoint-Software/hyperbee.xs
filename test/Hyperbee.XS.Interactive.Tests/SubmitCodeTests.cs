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

        var script = """
            #!xs

            var number = 123;
            number;
            """;

        await _kernel.SubmitCodeAsync( script );

        AssertSuccess( events );
        Assert.IsTrue( events.OfType<DisplayedValueProduced>().Any( x => (x.Value as string) == "123" ) );
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldAddPackage()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        var script =
            """
            #!xs

            package Humanizer.Core;
            using Humanizer;

            var x = 1+5;
            var y = x.ToWords();
            display(y);
            """;

        await _kernel.SubmitCodeAsync( script );

        AssertSuccess( events );
        Assert.IsTrue( events.OfType<DisplayedValueProduced>().Any( x => (x.Value as string) == "six" ) );
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldKeepVariables()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            """
            #!xs

            var x = 40;
            #!whos
            """ );

        var displayResult = GetDisplayResult( events );

        Assert.AreEqual( 1, displayResult.Length );
        Assert.AreEqual( "x:40", displayResult[0] );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            """
            #!xs
            x++;
            x++;

            #!whos
            """ );

        displayResult = GetDisplayResult( events );

        Assert.AreEqual( 1, displayResult.Length );
        Assert.AreEqual( "x:42", displayResult[0] );
    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldRedefineVariable()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            """
            #!xs

            var x = 123;
            var y = "hello";
            #!whos
            """ );

        var displayResult = GetDisplayResult( events );

        Assert.AreEqual( 2, displayResult.Length );
        Assert.AreEqual( "y:\"hello\"", displayResult[1] );
        Assert.AreEqual( "x:123", displayResult[0] );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            """
            #!xs

            var x = "world";
            #!whos
            """ );

        displayResult = GetDisplayResult( events );

        Assert.AreEqual( 2, displayResult.Length );
        Assert.AreEqual( "y:\"hello\"", displayResult[1] );
        Assert.AreEqual( "x:\"world\"", displayResult[0] );
        events.Clear();

    }

    [TestMethod]
    public async Task SubmitCode_WithCommand_ShouldShareVariablesWithKernels()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            """
            #!csharp

            var simple = "test";
            """ );

        events.Clear();

        await _kernel.SubmitCodeAsync(
            """
            #!xs

            #!share --from csharp --name "simple" --as "zSimple"
            #!whos
            """ );

        var displayResult = GetDisplayResult( events );

        Assert.AreEqual( 2, displayResult.Length );
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
