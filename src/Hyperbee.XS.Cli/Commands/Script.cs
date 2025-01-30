using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS;
using Hyperbee.XS.System.Writer;

namespace Hyperbee.Xs.Cli.Commands;

internal static class Script
{
    internal static string Execute( string script, IReadOnlyCollection<Assembly> references = null )
    {
        var parser = new XsParser(
            new XsConfig
            {
                References = references
            }
        );

        var expression = parser.Parse( script );

        var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
        var lambda = Expression.Lambda( delegateType, expression );
        var compiled = lambda.Compile();
        var result = compiled.DynamicInvoke();

        return result?.ToString() ?? "null";
    }

    internal static string Show( string script, IReadOnlyCollection<Assembly> references = null )
    {
        var parser = new XsParser(
            new XsConfig
            {
                References = references
            }
        );

        var expression = parser.Parse( script );

        return expression?.ToExpressionString() ?? "null";
    }
}
