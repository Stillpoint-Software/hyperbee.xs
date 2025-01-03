﻿using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserPropertyTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyResult()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.PropertyValue;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyChainingResult()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.PropertyThis.PropertyValue;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyMethodCallChainingResult()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.PropertyThis.AddNumbers( 10, 32 ).ToString();
            """ );

        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "42", result );
    }
}
