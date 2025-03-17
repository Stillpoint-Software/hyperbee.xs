using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserPropertyTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithPropertyResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.PropertyValue;
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
    public void Compile_ShouldSucceed_WithPropertyChainingResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.PropertyThis.PropertyValue;
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
    public void Compile_ShouldSucceed_WithPropertyMethodCallChainingResult( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.PropertyThis.AddNumbers( 10, 32 ).ToString();
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "42", result );
    }
}
