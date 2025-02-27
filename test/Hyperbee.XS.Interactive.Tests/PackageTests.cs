//#define NUGET_TESTS

#if NET9_0_OR_GREATER && NUGET_TESTS

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PackageManagement;
using System.Diagnostics;

namespace Hyperbee.XS.Interactive.Tests;

[TestClass]
public class PackageTests
{
    // Use constants, since parsing raw strings (""") with pragmas causes compiler error
    private const string Import = "#!import";
    private const string CSharpKernel = "#!csharp";
    private const string XsKernel = "#!xs";
    private const string Reference = "#r";
    private const string Source = "#i";

    private Kernel _kernel;
    private string _nugetPath;
    private string _nugetVersion;

    [TestInitialize]
    public async Task InitializeKernel()
    {
        _kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective((k, resolvedPackageReference) =>
                {
                    k.AddAssemblyReferences(resolvedPackageReference
                                                .SelectMany(r => r.AssemblyPaths));
                    return Task.CompletedTask;
                })
                .UseWho()
                .UseValueSharing()
        };


        var (path, version) = await SetupNuGet();
        _nugetPath = path;
        _nugetVersion = version;

        await new XsKernelExtension().OnLoadAsync( _kernel );
    }

    [TestCleanup]
    public void CleanUpKernel()
    {
        _kernel?.Dispose();
    }

    [TestMethod]
    public async Task SubmitCode_WithReference_ShouldAddPackageWithUseNugetDirective()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            $"""
            {XsKernel}
            {Reference} "nuget:Humanizer.Core"

            using Humanizer;

            var x = 1+5;
            var y = x.ToWords();
            display(y);
            """
        );

        AssertSuccess( events );
        Assert.IsTrue( events.OfType<DisplayedValueProduced>().Any( x => (x.Value as string) == "six" ) );
    }

    [TestMethod]
    public async Task SubmitCode_WithReference_ShouldAddExtensionFromSource()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            $$"""
            {{CSharpKernel}}
            {{Source}} "nuget:{{_nugetPath}}"
            {{Reference}} "nuget:Hyperbee.XS"
            {{Reference}} "nuget:Hyperbee.XS.Extensions"
            {{Reference}} "nuget:Hyperbee.XS.Interactive"
            {{Reference}} "nuget:Parlot"

            {{CSharpKernel}}
            using System.Collections.ObjectModel;
            using System.Linq.Expressions;
            using Hyperbee.Collections;
            using Hyperbee.Expressions;
            using Hyperbee.XS;
            using Hyperbee.XS.Core;
            using Hyperbee.XS.Core.Parsers;
            using Hyperbee.XS.Core.Writer;
            using Parlot.Fluent;
            using static Parlot.Fluent.Parsers;

            public class RepeatExpression : Expression
            {
                public override ExpressionType NodeType => ExpressionType.Extension;
                public override Type Type => typeof(void);
                public override bool CanReduce => true;

                public Expression Count { get; }
                public Expression Body { get; }

                public RepeatExpression(Expression count, Expression body)
                {
                    Count = count;
                    Body = body;
                }

                public override Expression Reduce()
                {
                    var loopVariable = Expression.Parameter(typeof(int), "i");
                    var breakLabel = Expression.Label();

                    return Expression.Block(
                        new[] { loopVariable },
                        Expression.Assign(loopVariable, Expression.Constant(0)),
                        Expression.Loop(
                            Expression.IfThenElse(
                                Expression.LessThan(loopVariable, Count),
                                Expression.Block(Body, Expression.PostIncrementAssign(loopVariable)),
                                Expression.Break(breakLabel)
                            ),
                            breakLabel
                        )
                    );
                }
            }

            public class RepeatParseExtension : IParseExtension
            {
                public ExtensionType Type => ExtensionType.Expression;
                public string Key => "repeat";

                public Parser<Expression> CreateParser( ExtensionBinder binder )
                {
                    var (expression, statement) = binder;

                    return Between(
                        Terms.Char('('),
                        expression,
                        Terms.Char(')')
                    )
                    .And( 
                         Between(
                            Terms.Char('{'),
                            statement,
                            Terms.Char('}')
                        )
                    )
                    .Then<Expression>( static parts =>
                    {
                        var (countExpression, body) = parts;
                        return new RepeatExpression(countExpression, body);
                    });
                }
            }

            var repeat = new RepeatParseExtension();
            """
        );

        AssertSuccess( events );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            $"""
            {XsKernel}
            {Import} --from csharp --name "repeat"
            """
        );
        AssertSuccess( events );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            $$"""
            {{XsKernel}}
            var x = 0;
            repeat (5) {
                x++;
            }
            x.ToString();
            """ );

        AssertSuccess( events );

        var value = events.OfType<DisplayedValueProduced>()
            .Select( x => x.Value )
            .OfType<string>()
            .First();

        Assert.AreEqual( "5", value );
    }

    [TestMethod]
    public async Task SubmitCode_WithReference_ShouldAddExtensionFromPackage()
    {
        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SubmitCodeAsync(
            $"""
            {XsKernel}

            source "{_nugetPath}";
            package Hyperbee.XS.Extensions:"{_nugetVersion}";
            """
        );
        AssertSuccess( events );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            $"""         
            {XsKernel}
            {Import} --extension ForParseExtension
            """
        );
        AssertSuccess( events );
        events.Clear();

        await _kernel.SubmitCodeAsync(
            $$"""
            {{XsKernel}}
            for ( var i = 0; i < 5; i++ )
            {
                display(i);
            }
            """
            );

        AssertSuccess( events );

        Assert.AreEqual( 6, events.OfType<DisplayedValueProduced>().Count() );
    }

    private static async Task<(string path, string version)> SetupNuGet()
    {
        var major = DateTime.UtcNow.Year.ToString();
        var minor = DateTime.UtcNow.Date.ToString( "MM" );
        var patch = DateTime.UtcNow.ToString( "ddhhmmss" );

        // Define paths
        string solutionDir = GetSolutionDirectory();  // Adjust as needed
        string nugetOutputDir = Path.Combine( solutionDir, ".nuget" );

        // Ensure directory exists
        if ( Directory.Exists( nugetOutputDir ) )
            Directory.Delete( nugetOutputDir, true );

        Directory.CreateDirectory( nugetOutputDir );

        // Run `dotnet pack` to generate the NuGet package
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"pack \"{solutionDir}\" /p:MajorVersion=\"{major}\" /p:MinorVersion=\"{minor}\" /p:PatchVersion=\"{patch}\" --output \"{nugetOutputDir}\" --configuration Debug",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.AreEqual( 0, process.ExitCode, $"dotnet pack failed: {error}" );

        return (nugetOutputDir.Replace( "\\", "/" ), $"{major}.{minor}.{patch}");

        static string GetSolutionDirectory()
        {
            var dir = Directory.GetCurrentDirectory(); // Starts in the test project directory

            while ( dir != null && Directory.GetFiles( dir, "*.sln" ).Length == 0 )
            {
                dir = Directory.GetParent( dir )?.FullName;
            }

            if ( dir == null )
            {
                throw new InvalidOperationException( "Solution directory not found." );
            }

            return dir;
        }
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

#endif
