using Hyperbee.XS.Core.Writer;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLambdaTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithResult()
    {
        var expression = Xs.Parse(
            """
            var myLambda = () => 1;
            myLambda();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithArgumentAndResult()
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => x;
            myLambda( 10 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WitStatementArgument()
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => { return x + 1; };
            myLambda( 12 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 13, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithReferenceArgument()
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( Hyperbee.XS.Tests.TestClass x ) => { return x.PropertyValue; };
            var myClass = new Hyperbee.XS.Tests.TestClass(42);
            myLambda( myClass );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodChaining()
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => { return x + 1; };
            (myLambda( 41 )).ToString();
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var es = lambda.ToExpressionString();

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( "42", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaArgument()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            var myClass = new Hyperbee.XS.Tests.Runnable( () => { x += 41; } );
            myClass.Run();
            ++x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaInvokeChaining()
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => 
            { 
                return ( int y ) => x + y + 1; 
            };
            myLambda(20)(21);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var es = lambda.ToExpressionString();

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidLambdaSyntax()
    {
        try
        {
            Xs.Parse(
                """
                var myLambda = ( int x => x;
                myLambda( 10 );
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidLambdaBody()
    {
        try
        {
            Xs.Parse(
                """
                var myLambda = ( int x ) => { return x + ; };
                myLambda( 10 );
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidLambdaParameter()
    {
        try
        {
            Xs.Parse(
                """
                var myLambda = ( int x, ) => x;
                myLambda( 10 );
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

}

public class Runnable( Func<int> run )
{
    public int Run() => run();
}
