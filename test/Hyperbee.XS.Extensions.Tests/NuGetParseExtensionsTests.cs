using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class NuGetParseExtensionTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        const string script =
            """
            nuget Humanizer.Core;
            import Humanizer;
            
            var number = 123;
            number.ToWords( default(System.Globalization.CultureInfo) );
            """;

        var xsConfig = new XsConfig()
        {
            Extensions = [new NuGetParseExtension()]
        };

        var xs = new XsParser( xsConfig );
        var expression = xs.Parse( script );

        var lambda = Expression.Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "one hundred and twenty-three", result );
    }
}
