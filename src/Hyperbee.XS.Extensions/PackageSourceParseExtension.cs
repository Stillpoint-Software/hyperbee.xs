using System.Linq.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class PackageSourceParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Directive;

    public string Key => "source";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Terms.String()
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( context, source ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

                xsContext.Resolver.ReferenceManager.AddSource( source.ToString() );

                return XsExpressionExtensions.Directive( $"source \"{source}\"" );
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
