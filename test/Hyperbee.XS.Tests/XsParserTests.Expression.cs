using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExpressionTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_Constant( CompilerType compiler )
    {
        var expression = Xs.Parse( "5;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_UnaryNegate( CompilerType compiler )
    {
        var expression = Xs.Parse( "-1 + 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_UnaryNot( CompilerType compiler )
    {
        var expression = Xs.Parse( "!false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_UnaryIsFalse( CompilerType compiler )
    {
        var expression = Xs.Parse( "!?false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_UnaryIsTrue( CompilerType compiler )
    {
        var expression = Xs.Parse( "?true;" );

        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_UnaryNot_Grouping( CompilerType compiler )
    {
        var expression = Xs.Parse( "!(false);" );

        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryAdd( CompilerType compiler )
    {
        var expression = Xs.Parse( "10 + 12;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 22, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultiple( CompilerType compiler )
    {
        var expression = Xs.Parse( "10 + 12 + 14;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 36, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryAdd_WithGrouping( CompilerType compiler )
    {
        var expression = Xs.Parse( "(10 + 12) * 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultipleGrouping( CompilerType compiler )
    {
        var expression = Xs.Parse( "(10 + 12) * (1 + 1);" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryLessThan( CompilerType compiler )
    {
        var expression = Xs.Parse( "10 < 11;" );

        var lambda = Lambda<Func<bool>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsTrue( result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryExponentiation( CompilerType compiler )
    {
        var expression = Xs.Parse( "2 ** 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 8, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_BinaryExponentiation_WithGrouping( CompilerType compiler )
    {
        var expression = Xs.Parse( "(2 + 3) ** 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 25, result );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidOperator( CompilerType compiler )
    {
        Assert.ThrowsExactly<SyntaxException>( () =>
        {
            try
        {
            Xs.Parse( "x = 5 $ 10;" );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
        } );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidMathOperator( CompilerType compiler )
    {
        Assert.ThrowsExactly<SyntaxException>( () =>
        {
            try
        {
            Xs.Parse( "5 ++ 10;" );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
        } );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithInvalidGrouping( CompilerType compiler )
    {
        Assert.ThrowsExactly<SyntaxException>( () =>
        {
            try
        {
            Xs.Parse( "(5 **) 2;" );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
        } );
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithMissingRight( CompilerType compiler )
    {
        Assert.ThrowsExactly<SyntaxException>( () =>
        {
            try
        {
            Xs.Parse( "5 +;" );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
        } );
    }
}
