using BenchmarkDotNet.Attributes;
using FastExpressionCompiler;
using static System.Linq.Expressions.Expression;
namespace Hyperbee.XS.Benchmark;

public class ScriptBenchmarks
{
    public XsParser Xs { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Xs = new();
    }

    // Compile

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "XS Execute" )]
    public void Hyperbee_Script_Compile()
    {
        var expression = Xs.Parse(
            """
            var x = 5;
            var result = if (x > 10)
                x *= 2;
            else
                x -= 2;

            result;
            """ );
        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();

        compiled();
    }

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "XS Execute Intrepet" )]
    public void Hyperbee_Script_CompileIntrepet()
    {
        var expression = Xs.Parse(
            """
            var x = 5;
            var result = if (x > 10)
                x *= 2;
            else
                x -= 2;

            result;
            """ );
        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile( preferInterpretation: true );

        compiled();
    }

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "XS Execute FEC" )]
    public void Hyperbee_Script_FastCompile()
    {
        var expression = Xs.Parse(
            """
            var x = 5;
            var result = if (x > 10)
                x *= 2;
            else
                x -= 2;

            result;
            """ );
        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.CompileFast();

        compiled();
    }

    // Execute

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "Native Execute", Baseline = true )]
    public void Native_Execute()
    {
        NativeTestAsync();
    }

    // Helpers

    public static int NativeTestAsync()
    {
        var x = 5;
        var result = (x > 10)
            ? x *= 2
            : x -= 2;

        return result;
    }
}
