using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLiteralTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    public void Parse_ShouldSucceed_WithStringLiteralDefault()
    {
        var expression = Xs.Parse( "\"Some String\";" );

        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "Some String", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithCharLiteralDefault()
    {
        var expression = Xs.Parse( "var x = 'c';" );
        var lambda = Lambda<Func<char>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 'c', result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteralDefault()
    {
        var expression = Xs.Parse( "var x = 12345;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteral()
    {
        var expression = Xs.Parse( "var x = 12345N;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongLiteral()
    {
        var expression = Xs.Parse( "var x = 12345L;" );
        var lambda = Lambda<Func<long>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345L, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithFloatLiteral()
    {
        var expression = Xs.Parse( "var x = 123.45F;" );
        var lambda = Lambda<Func<float>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45F, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithDoubleLiteral()
    {
        var expression = Xs.Parse( "var x = 123.45D;" );
        var lambda = Lambda<Func<double>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithDoubleLiteralResult()
    {
        var expression = Xs.Parse( "123.45D;" );
        var lambda = Lambda<Func<double>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLiteralMethodCallChaining()
    {
        var expression = Xs.Parse( "123.45D.ToString();" );
        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "123.45", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLiteralGroupingMethodCallChaining()
    {
        var expression = Xs.Parse( "(123.45D + 7D).ToString();" );
        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "130.45", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongAsForcedInt()
    {
        var expression = Xs.Parse( "var x = 12345L as int;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongAsLong()
    {
        var expression = Xs.Parse( "var x = 12345L as? long;" );
        var lambda = Lambda<Func<long?>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongAsInt()
    {
        var expression = Xs.Parse( "var x = 12345L as? int;" );
        var lambda = Lambda<Func<int?>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( null, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongAsIntFallback()
    {
        var expression = Xs.Parse( "var x = 12345L as? int ?? 10;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongIsLong()
    {
        var expression = Xs.Parse( "12345L is long;" );
        var lambda = Lambda<Func<bool>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithUnclosedString()
    {
        try
        {
            Xs.Parse(
                """
                var x = "Hello;
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
    public void Compile_ShouldFail_WithUnclosedParenthesis()
    {
        try
        {
            Xs.Parse(
                """
                var x = (5 + 10;
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
    public void Compile_ShouldFail_WithInvalidCharacter()
    {
        try
        {
            Xs.Parse(
                """
                var x = 5 @ 10;
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

