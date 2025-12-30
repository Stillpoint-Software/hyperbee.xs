using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserConditionalTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithoutBraces( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = if (true)
                1;
            else
                2;
            
            x;
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
    public void Compile_ShouldSucceed_WithConditional( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            if (true)
            {
                "hello";
            } 
            else
            { 
                "goodBye";
            }
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithConditionalAndNoElse( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = "goodbye";
            if (true)
            {
                x = "hello";
            }
            x; 
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithConditionalVariable( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 10;
            if ( x == (9 + 1) )
            {
                "hello";
            } 
            else
            { 
                "goodBye";
            }
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithConditionalAssignment( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var result = if (true)
            {
                "hello";
            } 
            else
            { 
                "goodBye";
            }
            result;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithMissingSemicolon( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var x = if (true)
                    1
                else
                    2;
                x;
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithUnmatchedBraces( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                if (true)
                {
                    "hello";
                else
                { 
                    "goodBye";
                }
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidCondition( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                if (true
                    "hello";
                else
                { 
                    "goodBye";
                }
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidElse( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                if (true)
                {
                    "hello";
                } 
                else
                    "goodBye"
                """ );
        } );
        Console.WriteLine( ex.Message );
    }
}

