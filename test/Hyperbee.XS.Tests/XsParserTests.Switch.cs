﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserSwitchTests
{
    public XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchCaseOnly()
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 1;
                    break;
                case 3:
                    result = 42;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchDefaultOnly()
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                default:
                    result = 42;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchCasesAndDefault()
    {
        var expression = Xs.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    break;
                case 2:
                    result = 20;
                    break;
                default:
                    result = 30;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchFallthroughToDefault()
    {
        var expression = Xs.Parse(
            """
            var x = 99;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    break;
                case 2:
                    result = 20;
                    break;
                case 3:
                    result = 30;
                    break;
                default:
                    result = 50;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 50, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchMultipleCaseLabels()
    {
        var expression = Xs.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                case 2:
                    result = 100;
                    break;
                default:
                    result = 0;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 100, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchNestedSwitchStatements()
    {
        var expression = Xs.Parse(
            """
            var x = 1;
            var y = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                    switch (y)
                    {
                        case 2:
                            result = 50;
                            break;
                        default:
                            result = 0;
                            break;
                    }
                    break;
                default:
                    result = 0;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 50, result );
    }
}

