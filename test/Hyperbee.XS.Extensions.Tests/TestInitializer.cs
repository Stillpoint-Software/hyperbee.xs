using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
using Hyperbee.Xs.Extensions.Lab;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class TestInitializer
{
    public static XsConfig XsConfig { get; set; }

    [AssemblyInitialize]
    public static void Initialize( TestContext _ )
    {
        var typeResolver = TypeResolver.Create( Assembly.GetExecutingAssembly() );

        XsConfig = new XsConfig( typeResolver )
        {
            Extensions = [.. XsExtensions.Extensions(), new FetchParseExtension(), new JsonParseExtension(), new RegexParseExtension()]
        };
    }
}
