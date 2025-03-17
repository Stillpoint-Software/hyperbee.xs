using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserMethodTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodCall( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodCallArgs( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.AddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithGenericMethodCall( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.GenericAdd<int>(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithGenericMethodCallTypeInference( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.GenericAdd(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodCallChaining( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.MethodThis().AddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithMethodCallPropertyChaining( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.MethodThis().PropertyThis.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithStaticMethodCallArgs( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = Hyperbee.XS.Tests.TestClass.StaticAddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithExtensionMethods( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new int[] {1,2,3,4,5};
            x.Select( ( int i ) => i * 10 ).Sum();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 150, result );
    }
}

