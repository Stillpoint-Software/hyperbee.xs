using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.System;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class StringFormatParseExtensionTests
{
    public static XsParser Xs { get; set; } = new
    (
        new XsConfig
        {
            ReferenceManager = ReferenceManager.Create( Assembly.GetExecutingAssembly() ),
            Extensions = XsExtensions.Extensions()
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse(
            """
            var x = "hello";
            var y = "!";
            var result = `{x} world{y}`;
            result;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello world!", result );
    }
}


