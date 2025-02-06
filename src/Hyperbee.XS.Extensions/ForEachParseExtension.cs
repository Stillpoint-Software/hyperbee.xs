using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Hyperbee.XS.System.Writer;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class ForEachParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "foreach";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            XsParsers.Bounded(
                static ctx =>
                {
                    ctx.EnterScope( FrameType.Block );
                },
                Between(
                    Terms.Char( '(' ),
                        Terms.Text( "var" )
                            .SkipAnd( Terms.Identifier() )
                            .AndSkip( Terms.Text( "in" ) )
                            .And( expression ),
                    Terms.Char( ')' )
                ).Then( static ( ctx, parts ) =>
                {
                    var (elementIdentifier, collection) = parts;

                    var elementName = elementIdentifier.ToString()!;
                    var elementVariable = Variable(
                        collection.Type.GetElementType()!,
                        elementName );

                    ctx.Scope().Variables
                        .Add( elementName, elementVariable );

                    return (elementVariable, collection);
                } )
                .And( statement )
                .Then<Expression>( static parts =>
                {
                    var ((element, collection), body) = parts;

                    return ExpressionExtensions.ForEach( collection, element, body );
                } ),
                static ctx =>
                {
                    ctx.ExitScope();
                }
            ).Named( "foreach" );
    }

    public bool CanWrite( Expression node )
    {
        return node is ForEachExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not ForEachExpression forEachExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.ForEach", true, false );

        writer.WriteExpression( forEachExpression.Collection );
        writer.Write( ",\n" );
        writer.WriteExpression( forEachExpression.Element );
        writer.Write( ",\n" );
        writer.WriteExpression( forEachExpression.Body );
        writer.Write( "\n" );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not ForEachExpression forEachExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "foreach( ", indent: true );
        writer.WriteExpression( forEachExpression.Element );
        writer.Write( " in " );
        writer.WriteExpression( forEachExpression.Collection );
        writer.Write( " )\n" );
        writer.WriteExpression( forEachExpression.Body );
    }
}
