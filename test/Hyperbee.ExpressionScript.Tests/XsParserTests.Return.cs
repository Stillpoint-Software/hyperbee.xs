﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserReturnTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithVoidReturn()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 10;
            if (true)
            {
                return;
            }
            return;
            """ );

        var lambda = Lambda<Action>( expression );
        var compiled = lambda.Compile();

        // Assert no exceptions occur
        compiled();
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithIntReturn()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 10;
            if (true)
            {
                return 42;
            }
            10; // This is the last evaluated expression
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldFail_WithVoidAndIntReturnTypeMismatch()
    {
        try
        {
            var parser = new XsParser();
            var expresison = parser.Parse(
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
    public void Compile_ShouldFail_WithMixedReturnTypes()
    {
        try
        {
            var parser = new XsParser();
            parser.Parse(
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
    public void Compile_ShouldSucceed_WithMatchingNestedReturns()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithReturnInLoop()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithReturnInSwitch()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVoidReturnInSwitch()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
        var compiled = lambda.Compile();

        // Assert no exceptions occur
        compiled();
    }
}
