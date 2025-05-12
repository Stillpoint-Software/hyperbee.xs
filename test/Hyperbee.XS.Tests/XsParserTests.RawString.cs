using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserRawStringTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithRawStringLiteral( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """"
            var x = """Raw string with "With Quotes".""";
            x;
            """" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "Raw string with \"With Quotes\".", result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithSingleRawString( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """"
            var x = """!""";
            x;
            """" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( "!", result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Parse_ShouldSucceed_WithRawStringWithRawString( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """"""
            var x = """"Raw string with """With Raw String"""."""";
            x;
            """""" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( """"Raw string with """With Raw String"""."""", result );
    }
}
