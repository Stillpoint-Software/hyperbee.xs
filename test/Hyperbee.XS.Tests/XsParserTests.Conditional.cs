using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserConditionalTests
{
    public static XsParser Xs { get; } = new();

    [DataTestMethod]
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

    [DataTestMethod]
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

    [DataTestMethod]
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

    [DataTestMethod]
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

    [DataTestMethod]
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

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithMissingSemicolon( CompilerType compiler )
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

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithUnmatchedBraces( CompilerType compiler )
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

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidCondition( CompilerType compiler )
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

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidElse( CompilerType compiler )
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

