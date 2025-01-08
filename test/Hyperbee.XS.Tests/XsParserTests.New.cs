﻿using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserNewExpressionTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig { References = [Assembly.GetExecutingAssembly()] }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewExpression()
    {
        var expression = Xs.Parse(
            """
            new Hyperbee.XS.Tests.TestClass(42);
            """ );

        var lambda = Expression.Lambda<Func<TestClass>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.PropertyValue );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewAndProperty()
    {
        try
        {
            var expression = Xs.Parse(
                """
                new Hyperbee.XS.Tests.TestClass(42).PropertyThis.PropertyValue;
                """ );

            var lambda = Expression.Lambda<Func<int>>( expression );

            var compiled = lambda.Compile();
            var result = compiled();

            Assert.AreEqual( 42, result );
        }
        catch ( SyntaxException se )
        {
            Assert.Fail( se.Message );
        }
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewArray()
    {
        var expression = Xs.Parse(
            """
            new int[5];
            """ );

        var lambda = Expression.Lambda<Func<int[]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewMultiDimensionalArray()
    {
        var expression = Xs.Parse(
            """
            new int[2,5];
            """ );

        var lambda = Expression.Lambda<Func<int[,]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewArrayInit()
    {
        var parser = new XsParser();

        var expression = parser.Parse(
            """
            new int[] {1,2};
            """ );

        var lambda = Expression.Lambda<Func<int[]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewJaggedArray()
    {
        var parser = new XsParser();

        var expression = parser.Parse(
            """
            new int[] { new int[] {10,20,30}, new int[] {40,50}, new int[] {60} };
            """ );

        var lambda = Expression.Lambda<Func<int[][]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 3, result.Length );
        Assert.AreEqual( 3, result[0].Length );
        Assert.AreEqual( 10, result[0][0] );
        Assert.AreEqual( 2, result[1].Length );
        Assert.AreEqual( 40, result[1][0] );
        Assert.AreEqual( 1, result[2].Length );
        Assert.AreEqual( 60, result[2][0] );

    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGeneric()
    {
        var expression = Xs.Parse(
            """
            new List<int>();
            """ );

        var lambda = Expression.Lambda<Func<List<int>>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsInstanceOfType<List<int>>( result );
    }
}
