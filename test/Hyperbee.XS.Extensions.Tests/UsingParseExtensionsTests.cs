﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class UsingParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse(
            """
            using Hyperbee.XS.Extensions.Tests;
            
            var x = 0;
            var onDispose = () => { x++; };
            using( var disposable = new Disposable(onDispose) )
            {
                x++;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result );
    }
}

public class Disposable( Func<int> onDispose ) : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        onDispose();
    }
}



