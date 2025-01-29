﻿using System.Reflection;
using Hyperbee.Xs.Extensions;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class ForEachParseExtensionTests
{
    public XsParser XsParser { get; set; } = new
    (
        new XsConfig
        {
            References = [Assembly.GetExecutingAssembly()],
            Extensions = XsExtensions.Extensions()
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = XsParser.Parse(
            """
            var array = new int[] { 1,2,3 };
            var x = 0;
            foreach ( var item in array )
            {
                x = x + item;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 6, result );
    }
}



