using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserIndexTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithIndexResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMultiDimensionalIndexResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[32,10];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithIndexChaining( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            new Hyperbee.XS.Tests.TestClass(-1)[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodChainingIndexResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.MethodThis()[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithUnclosedBracket( CompilerType compiler )
    {
        try
        {
            Xs.Parse(
                """
                var x = new int[5;
                x;
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

}
