using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
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

public enum CompilerType
{
    Fast,
    System,
    Interpret
}

public static class TestExtensions
{
    public static Delegate Compile( this LambdaExpression expression, CompilerType compilerType = CompilerType.System )
    {
        return compilerType switch
        {
            CompilerType.Fast => expression.CompileFast(),
            CompilerType.System => expression.Compile(),
            CompilerType.Interpret => expression.Compile( preferInterpretation: true ),
            _ => throw new ArgumentOutOfRangeException( nameof( compilerType ), compilerType, null )
        };
    }

    public static T Compile<T>( this Expression<T> expression, CompilerType compilerType = CompilerType.System )
        where T : Delegate
    {
        return compilerType switch
        {
            CompilerType.Fast => expression.CompileFast(),
            CompilerType.System => expression.Compile(),
            CompilerType.Interpret => expression.Compile( preferInterpretation: true ),
            _ => throw new ArgumentOutOfRangeException( nameof( compilerType ) )
        };
    }
}
