using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserIndexTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithIndexResult()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMultiDimensionalIndexResult()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[32,10];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithIndexChaining()
    {
        var expression = Xs.Parse(
            """
            new Hyperbee.XS.Tests.TestClass(-1)[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodChainingIndexResult()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.MethodThis()[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithUnclosedBracket()
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
