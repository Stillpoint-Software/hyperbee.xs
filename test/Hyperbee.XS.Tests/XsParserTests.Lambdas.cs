using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLambdaTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var myLambda = () => 1;
            myLambda();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithArgumentAndResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => x;
            myLambda( 10 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithReturnStatement( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => { return x + 1; };
            myLambda( 12 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 13, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithReferenceArgument( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( Hyperbee.XS.Tests.TestClass x ) => { return x.PropertyValue; };
            var myClass = new Hyperbee.XS.Tests.TestClass(42);
            myLambda( myClass );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodChaining( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var myLambda = ( int x ) => { return x + 1; };
            (myLambda( 41 )).ToString();
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "42", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithLambdaArgument( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            var myClass = new Hyperbee.XS.Tests.Runnable( () => { x += 41; } );
            myClass.Run();
            ++x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithLambdaInvokeChaining( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidLambdaSyntax( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var myLambda = ( int x => x;
                myLambda( 10 );
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidLambdaBody( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var myLambda = ( int x ) => { return x + ; };
                myLambda( 10 );
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidLambdaParameter( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var myLambda = ( int x, ) => x;
                myLambda( 10 );
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

}

public class Runnable( Func<int> run )
{
    public int Run() => run();
}
