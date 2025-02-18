using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.Core;
using Hyperbee.XS.Interpreter;

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

public static class TestExtensions
{
    public static T CompileEx<T>( this Expression<T> expression, bool preferInterpret = false )
        where T : Delegate
    {
        return preferInterpret ? expression.Interpreter() : expression.Compile();
    }
}
