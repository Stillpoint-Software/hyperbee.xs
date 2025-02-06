using System.Reflection;
using Hyperbee.XS.System;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserPropertyTests
{
    public static XsParser Xs { get; set; } = new
    (
        new XsConfig { ReferenceManager = ReferenceManager.Create( Assembly.GetExecutingAssembly() ) }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyResult()
    {
        var expression = Xs.Parse(
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
        var expression = Xs.Parse(
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
        var expression = Xs.Parse(
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
