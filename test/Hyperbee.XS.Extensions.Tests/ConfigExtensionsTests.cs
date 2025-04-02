using Hyperbee.Expressions;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class ConfigExtensionsTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithConfig()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            config<string>("Hello");
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( "Hello, World!", result );
    }
}
