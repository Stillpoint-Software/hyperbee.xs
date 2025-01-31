using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Hyperbee.XS.System.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class AsyncParseExtension : IParseExtension, IExtensionWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "async";

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
            ).Named( "async block" )
            .Then<Expression>( static ( ctx, parts ) =>
                ExpressionExtensions.BlockAsync(
                    [.. ctx.Scope().Variables.EnumerateValues( Collections.KeyScope.Current )],
                    [.. parts]
                )
            ),
            static ctx =>
            {
                ctx.ExitScope();
            }
        ).Named( "async" );
    }

    public bool CanWrite( Expression node )
    {
        return node is AsyncBlockExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not AsyncBlockExpression asyncBlock )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.BlockAsync", true, false );

        var variableCount = asyncBlock.Variables.Count;

        if ( variableCount > 0 )
        {
            writer.WriteParamExpressions( asyncBlock.Variables, true );
        }

        var expressionCount = asyncBlock.Expressions.Count;

        if ( expressionCount != 0 )
        {
            if ( variableCount > 0 )
            {
                writer.Write( ",\n" );
            }

            for ( var i = 0; i < expressionCount; i++ )
            {
                writer.WriteExpression( asyncBlock.Expressions[i] );
                if ( i < expressionCount - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }
        }
    }
}
