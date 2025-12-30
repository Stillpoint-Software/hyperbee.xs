using System.Linq.Expressions;
using FastExpressionCompiler;

using Hyperbee.XS.Core.Writer;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLiteralTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithStringLiteralDefault( CompilerType compiler )
    {
        var expression = Xs.Parse( "\"Some String\";" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "Some String", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithCharLiteralDefault( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 'c';" );
        var lambda = Lambda<Func<char>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 'c', result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithIntegerLiteralDefault( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345;" );
        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithIntegerLiteral( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345N;" );
        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongLiteral( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345L;" );
        var lambda = Lambda<Func<long>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 12345L, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithFloatLiteral( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 123.45F;" );
        var lambda = Lambda<Func<float>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 123.45F, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithDoubleLiteral( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 123.45D;" );
        var lambda = Lambda<Func<double>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithDoubleLiteralResult( CompilerType compiler )
    {
        var expression = Xs.Parse( "123.45D;" );
        var lambda = Lambda<Func<double>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLiteralMethodCallChaining( CompilerType compiler )
    {
        var expression = Xs.Parse( "123.45D.ToString();" );
        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "123.45", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLiteralGroupingMethodCallChaining( CompilerType compiler )
    {
        var expression = Xs.Parse( "(123.45D + 7D).ToString();" );
        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "130.45", result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongAsForcedInt( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345L as int;" );
        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    //[DataRow( CompilerType.Fast )]  // Issue: https://github.com/dadhi/FastExpressionCompiler/pull/456
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongAsLong( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345L as? long;" );
        var lambda = Lambda<Func<long?>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    //[DataRow( CompilerType.Fast )]  // Issue: https://github.com/dadhi/FastExpressionCompiler/pull/456
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongAsInt( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345L as? int;" );
        var lambda = Lambda<Func<int?>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsNull( result );
    }

    [TestMethod]
    //[DataRow( CompilerType.Fast )]  // Issue: https://github.com/dadhi/FastExpressionCompiler/pull/456
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongAsIntFallback( CompilerType compiler )
    {
        var expression = Xs.Parse( "var x = 12345L as? int ?? 10;" );
        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    //[DataRow( CompilerType.Fast )]  // Issue: https://github.com/dadhi/FastExpressionCompiler/pull/456
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithLongIsLong( CompilerType compiler )
    {
        var expression = Xs.Parse( "12345L is long;" );
        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithUnclosedString( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var x = "Hello;
                x;
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithUnclosedParenthesis( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var x = (5 + 10;
                x;
                """ );
        } );
        Console.WriteLine( ex.Message );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidCharacter( CompilerType compiler )
    {
        var ex = Assert.Throws<SyntaxException>( () =>
        {
            Xs.Parse(
                """
                var x = 5 @ 10;
                x;
                """ );
        } );
        Console.WriteLine( ex.Message );
    }
}

