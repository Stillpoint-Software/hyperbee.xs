using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;

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
            Extensions = XsExtensions.Extensions()
        };
    }
}
