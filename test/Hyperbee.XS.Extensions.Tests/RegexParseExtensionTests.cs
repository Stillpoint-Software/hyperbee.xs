using Hyperbee.Xs.Extensions.Lab;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class RegexParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( GetXsConfig() );

    private static XsConfig GetXsConfig()
    {
        var config = TestInitializer.XsConfig;
        config.Extensions.Add( new RegexParseExtension() );
        return config;
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithRegex()
    {
        var expression = Xs.Parse(
            """
            regex "Find world in string"::/world/[0].Value;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "world", result );
    }
}
