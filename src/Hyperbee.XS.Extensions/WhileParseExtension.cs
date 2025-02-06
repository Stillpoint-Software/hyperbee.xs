using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class WhileParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "while";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            Between(
                Terms.Char( '(' ),
                expression,
                Terms.Char( ')' )
            )
            .And( statement )
            .Then<Expression>( static parts =>
            {
                var (test, body) = parts;
                return ExpressionExtensions.While( test, body );
            } )
            .Named( "while" );
    }

    public bool CanWrite( Expression node )
    {
        return node is WhileExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not WhileExpression whileExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.While", true, false );

        writer.WriteExpression( whileExpression.Test );
        writer.Write( ",\n" );
        writer.WriteExpression( whileExpression.Body );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not WhileExpression whileExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "while (" );
        writer.WriteExpression( whileExpression.Test );
        writer.Write( ")\n" );
        writer.WriteExpression( whileExpression.Body );
    }
}
