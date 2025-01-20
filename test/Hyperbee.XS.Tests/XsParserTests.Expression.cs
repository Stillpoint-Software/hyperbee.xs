﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExpressionTests
{
    public XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_Constant()
    {
        var expression = Xs.Parse( "5;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNegate()
    {
        var expression = Xs.Parse( "-1 + 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot()
    {
        var expression = Xs.Parse( "!false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot_Grouping()
    {
        var expression = Xs.Parse( "!(false);" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd()
    {
        var expression = Xs.Parse( "10 + 12;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 22, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultiple()
    {
        var expression = Xs.Parse( "10 + 12 + 14;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 36, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithGrouping()
    {
        var expression = Xs.Parse( "(10 + 12) * 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMulitpleGrouping()
    {
        var expression = Xs.Parse( "(10 + 12) * (1 + 1);" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryLessThan()
    {
        var expression = Xs.Parse( "10 < 11;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryExponentiation()
    {
        var expression = Xs.Parse( "2 ** 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 8, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryExponentiation_WithGrouping()
    {
        var expression = Xs.Parse( "(2 + 3) ** 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 25, result );
    }
}

