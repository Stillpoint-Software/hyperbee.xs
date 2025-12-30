using Hyperbee.XS.Core.Writer;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserReturnTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithVoidReturn( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 10;
            if (true)
            {
                return;
            }
            return;
            """ );

        var lambda = Lambda<Action>( expression );

        var function = lambda.Compile( compiler );
        function(); // No exceptions should be thrown
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithIntReturn( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 10;
            if (true)
            {
                return 42;
            }
            10; // This is the last evaluated expression
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
    public void Compile_ShouldFail_WithVoidAndIntReturnTypeMismatch( CompilerType compiler )
    {
        try
        {
            Xs.Parse(
                """
                var x = 10;
                if (true)
                {
                    return;
                }
                x = 20; // Type mismatch: void return and int assignment
                """ );

            Assert.Fail( "Expected an exception for mismatched return types." );
        }
        catch ( InvalidOperationException ex )
        {
            StringAssert.Contains( ex.Message, "Mismatched return types" );
        }
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldFail_WithMixedReturnTypes( CompilerType compiler )
    {
        try
        {
            Xs.Parse(
                """
                var x = 10;
                if (true)
                {
                    return 42;
                }
                return; // Mismatched: void vs int
                """ );

            Assert.Fail( "Expected an exception for mismatched return types." );
        }
        catch ( InvalidOperationException ex )
        {
            StringAssert.Contains( ex.Message, "Mismatched return types" );
        }
    }

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMatchingNestedReturns( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 10;
            if (true)
            {
                if (false)
                {
                    return 100;
                }
                return 42;
            }
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
    public void Compile_ShouldSucceed_WithReturnInLoop( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var result = 0;
            loop
            {
                result = 10;
                return 42;
            }
            return result;
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
    public void Compile_ShouldSucceed_WithReturnInSwitch( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            switch (x)
            {
                case 1:
                    return 10;
                case 3:
                    return 42;
                default:
                    return 0;
            }
            return x;
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
    public void Compile_ShouldSucceed_WithVoidReturnInSwitch( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            switch (x)
            {
                case 1:
                    return;
                case 3:
                    return;
                default:
                    return;
            }
            return;
            """ );

        var lambda = Lambda<Action>( expression );

        var function = lambda.Compile( compiler );
        function(); // No exceptions should be thrown
    }
}
