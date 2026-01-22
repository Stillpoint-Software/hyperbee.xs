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

public class EnumerableParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "enumerable";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, statement) = binder;

        return XsParsers.Bounded(
            static ctx =>
            {
                ctx.EnterScope( FrameType.Block );
            },
            Between(
                // This is basically a block, but we need the parts
                Terms.Char( '{' ),
                ZeroOrMany( statement ),
                Terms.Char( '}' )
            ).Named( "enumerable block" )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var vars = new System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression>(
                    [.. ctx.Scope().Variables.EnumerateValues( LinkedNode.Current )]
                );
                var exprs = new System.Collections.ObjectModel.ReadOnlyCollection<Expression>(
                    [.. parts]
                );
                return ExpressionExtensions.BlockEnumerable( vars, exprs );
            } ),
            static ctx =>
            {
                ctx.ExitScope();
            }
        ).Named( "enumerable" );
    }

    public bool CanWrite( Expression node )
    {
        return node is EnumerableBlockExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not EnumerableBlockExpression enumerableBlockExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.BlockEnumerable", true, false );

        var variableCount = enumerableBlockExpression.Variables.Count;

        if ( variableCount > 0 )
        {
            writer.WriteParamExpressions( enumerableBlockExpression.Variables, true );
        }

        var expressionCount = enumerableBlockExpression.Expressions.Count;

        if ( expressionCount != 0 )
        {
            if ( variableCount > 0 )
            {
                writer.Write( ",\n" );
            }

            for ( var i = 0; i < expressionCount; i++ )
            {
                writer.WriteExpression( enumerableBlockExpression.Expressions[i] );
                if ( i < expressionCount - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }
        }
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not EnumerableBlockExpression enumerableBlockExpression )
            return;

        using var writer = context.GetWriter();

        var expressionCount = enumerableBlockExpression.Expressions.Count;

        writer.Write( "enumerable {\n" );
        writer.Indent();

        for ( var i = 0; i < expressionCount; i++ )
        {
            writer.WriteExpression( enumerableBlockExpression.Expressions[i] );
            writer.WriteTerminated();
        }

        writer.Outdent();
        writer.Write( "}\n" );
        context.SkipTerminated = true;
    }
}
