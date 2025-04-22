using System.Linq.Expressions;
using System.Text.Json;
using Hyperbee.Expressions.Lab;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;

using static Parlot.Fluent.Parsers;
using ExpressionExtensions = Hyperbee.Expressions.Lab.ExpressionExtensions;

namespace Hyperbee.Xs.Extensions.Lab;

public class FetchParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "fetch";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;
        // var response = fetch("name", "URL" );

        return If(
                ctx => ctx.StartsWith( "(" ),
                Between(
                    Terms.Char( '(' ),
                    Separated(
                        Terms.Char( ',' ),
                        expression
                    ),
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( static parts => parts.Count switch
            {
                4 => ExpressionExtensions.Fetch( clientName: parts[0], url: parts[1], method: parts[2], content: parts[3], headers: parts[4] ),
                3 => ExpressionExtensions.Fetch( clientName: parts[0], url: parts[1], method: parts[2], content: parts[3] ),
                _ => ExpressionExtensions.Fetch( clientName: parts[0], url: parts[1] )
            } )
            .Named( "fetch" );
    }

    public bool CanWrite( Expression node )
    {
        return node is FetchExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not FetchExpression fetchExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.Lab.Fetch", true, false );

        writer.WriteExpression( fetchExpression.ClientName );
        writer.Write( ",\n" );
        writer.WriteExpression( fetchExpression.Url );
        writer.Write( ",\n" );
        writer.Write( fetchExpression.Type, indent: true );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not FetchExpression fetchExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "fetch(" );
        writer.WriteExpression( fetchExpression.ClientName );
        writer.WriteExpression( fetchExpression.Url );
        writer.Write( ")" );
    }
}
