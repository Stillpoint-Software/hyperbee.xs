using System.Linq.Expressions;
using Hyperbee.Collections;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class InjectParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "inject";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Between(
                Terms.Char( '<' ),
                XsParsers.TypeRuntime(),
                Terms.Char( '>' )
            )
            .And(
                Between(
                    Terms.Char( '(' ),
                    ZeroOrOne( Terms.String( StringLiteralQuotes.Double ) ),
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( static ( _, parts ) =>
                {
                    var (type, key) = parts;
                    return ExpressionExtensions.Inject( type, key.ToString() );
                }
            )
            .Named( "inject" );
    }

    public bool CanWrite( Expression node )
    {
        return node is InjectExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not InjectExpression injectExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.Inject", true, false );

        writer.WriteType( injectExpression.Type );

        if ( injectExpression.Key != null )
        {
            writer.Write( ",\n" );
            writer.Write( $"\"{injectExpression.Key}\"", indent: true );
        }
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not InjectExpression injectExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "inject<" );
        writer.WriteType( injectExpression.Type );
        writer.Write( ">" );

        if ( injectExpression.Key != null )
        {
            writer.Write( "(" );
            writer.Write( $"\"{injectExpression.Key}\"" );
            writer.Write( ")" );
        }
        else
        {
            writer.Write( "()" );
        }
    }
}
