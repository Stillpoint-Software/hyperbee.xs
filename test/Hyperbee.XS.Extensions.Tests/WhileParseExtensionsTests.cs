﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class WhileParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse(
            """
            var running = true;
            var x = 0;
            while ( running )
            {    
                x++;
                if ( x == 10 )
                { 
                    running = false;
                }
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }
}



