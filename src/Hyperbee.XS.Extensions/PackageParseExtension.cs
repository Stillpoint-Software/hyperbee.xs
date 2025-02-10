using System.Linq.Expressions;
using Hyperbee.Xs.Extensions.Core;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class PackageParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Directive;

    public string Key => "package";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Terms.NamespaceIdentifier()
            .And(
                ZeroOrOne(
                    Terms.Char( ':' ).SkipAnd( Terms.Identifier() )
                )
            )
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( context, parts ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

                var packageId = parts.Item1.ToString();
                var version = parts.Item2.ToString();

                AsyncCurrentThreadHelper.RunSync( async () =>
                {
                    var resolver = xsContext.Resolver;
                    var assemblies = await resolver.ReferenceManager.LoadPackageAsync( packageId, version );
                    resolver.RegisterExtensionMethods( assemblies );
                } );

                return XsExpressionExtensions.Directive( $"package {packageId}{(!string.IsNullOrWhiteSpace( version ) ? $":{version}" : string.Empty)}" );
            } );
    }

    public bool CanWrite( Expression node )
    {
        return node is DirectiveExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not DirectiveExpression directiveExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.XS.XsExpressionExtensions.Directive", true, false );
        writer.Write( $"\"{directiveExpression.Directive}\"", indent: true );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not DirectiveExpression directiveExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( directiveExpression.Directive );
    }
}

