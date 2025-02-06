using System.Collections.ObjectModel;
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

public class ForParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "for";

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
                    expression.AndSkip( Terms.Char( ';' ) )
                            .And( expression ).AndSkip( Terms.Char( ';' ) )
                            .And( expression ),
                    Terms.Char( ')' )
                )
                .And( statement )
                .Then<Expression>( static ( ctx, parts ) =>
                {
                    var ((initialization, test, iteration), body) = parts;

                    // Call ToArray to ensure the variables remain in scope for reduce.
                    var variables = ctx.Scope().Variables
                        .EnumerateValues( KeyScope.Current ).ToArray();

                    return ExpressionExtensions.For( variables, initialization, test, iteration, body );
                } ),
                static ctx =>
                {
                    ctx.ExitScope();
                }
            ).Named( "for" );
    }

    public bool CanWrite( Expression node )
    {
        return node is ForExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not ForExpression forExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.For", true, false );

        var variables = new ReadOnlyCollection<ParameterExpression>( forExpression.Variables.ToList() );

        writer.WriteParamExpressions( variables, firstArgument: true );
        writer.Write( ",\n" );
        writer.WriteExpression( forExpression.Initialization );
        writer.Write( ",\n" );
        writer.WriteExpression( forExpression.Test );
        writer.Write( ",\n" );
        writer.WriteExpression( forExpression.Iteration );
        writer.Write( ",\n" );
        writer.WriteExpression( forExpression.Body );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not ForExpression forExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "for( ", indent: true );
        writer.WriteExpression( forExpression.Initialization );
        writer.Write( "; " );
        writer.WriteExpression( forExpression.Test );
        writer.Write( "; " );
        writer.WriteExpression( forExpression.Iteration );
        writer.Write( ")\n" );
        writer.WriteExpression( forExpression.Body );
    }
}
