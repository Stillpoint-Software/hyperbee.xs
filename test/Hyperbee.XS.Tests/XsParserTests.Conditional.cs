using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserConditionalTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_WithoutBraces()
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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditional()
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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAndNoElse()
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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalVariable()
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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAssignment()
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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithMissingSemicolon()
    {
        try
        {
            Xs.Parse(
                """
                var x = if (true)
                    1
                else
                    2;
                x;
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
    public void Compile_ShouldFail_WithUnmatchedBraces()
    {
        try
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
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidCondition()
    {
        try
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
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidElse()
    {
        try
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
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }
}

