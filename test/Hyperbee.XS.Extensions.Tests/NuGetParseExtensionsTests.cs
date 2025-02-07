using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class NuGetParseExtensionTests
{
    [TestMethod]
    public async Task Compile_ShouldSucceed_WithExtensions()
    {
        const string script =
            """
            import Humanizer;
            
            var number = 123;
            number.ToWords( default(System.Globalization.CultureInfo) );
            """;

        var rm = new ReferenceManager();
        await rm.LoadPackageAsync( "Humanizer.Core" );

        var xsConfig = new XsConfig
        {
            ReferenceManager = rm
        };

        var xs = new XsParser( xsConfig );

        var expression = xs.Parse( script );

        var lambda = Expression.Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();
    }
}
