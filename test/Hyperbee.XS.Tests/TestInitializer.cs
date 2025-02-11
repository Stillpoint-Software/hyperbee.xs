using System.Reflection;
using Hyperbee.XS.Core;

namespace Hyperbee.XS.Tests;

[TestClass]
public static class TestInitializer
{
    public static XsConfig XsConfig { get; set; }

    [AssemblyInitialize]
    public static void Initialize( TestContext _ )
    {
        var typeResolver = TypeResolver.Create( Assembly.GetExecutingAssembly() );

        XsConfig = new XsConfig( typeResolver );
    }
}
