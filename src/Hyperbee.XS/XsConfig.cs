using System.Linq.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;

namespace Hyperbee.XS;

public class XsConfig( TypeResolver resolver = null )
{
    public List<IParseExtension> Extensions { get; init; } = [];
    internal TypeResolver Resolver { get; } = resolver ?? new TypeResolver( new ReferenceManager() );
}

internal static class XsConfigExtensions
{
    public static KeywordParserPair<Expression>[] Statements(
        this IReadOnlyCollection<IParseExtension> extensions,
        ExtensionType type,
        Parser<Expression> expression,
        Deferred<Expression> statement )
    {
        var binder = new ExtensionBinder( expression, statement );

        return extensions
            .Where( x => type.HasFlag( x.Type ) )
            .OrderBy( x => x.Type )
            .Select( x => new KeywordParserPair<Expression>( x.Key, x.CreateParser( binder ) ) )
            .ToArray();
    }

    public static Parser<Expression>[] Literals(
        this IReadOnlyCollection<IParseExtension> extensions,
        Parser<Expression> expression )
    {
        var binder = new ExtensionBinder( expression, default );

        return extensions
            .Where( x => ExtensionType.Literal.HasFlag( x.Type ) )
            .OrderBy( x => x.Type )
            .Select( x => x.CreateParser( binder ) )
            .ToArray();
    }
}
