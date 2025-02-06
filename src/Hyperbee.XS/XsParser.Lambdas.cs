using System.Linq.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    private static Parser<Expression> LambdaParser( Parser<Expression> typeConstant, Deferred<Expression> expression )
    {
        return If(
                ctx => ctx.StartsWith( "(" ),
                Between(
                    OpenParen,
                    Parameters( typeConstant ),
                    CloseParen
                )
            )
            .AndSkip( Terms.Text( "=>" ) )
            .And(
                expression
                .Then( static ( ctx, body ) =>
                {
                    var (_, _, frame) = ctx;
                    var returnLabel = frame.ReturnLabel;

                    if ( returnLabel != null )
                    {
                        return Block(
                            body,
                            Label( returnLabel, Default( returnLabel.Type ) )
                        );
                    }
                    return body;
                } )
            )
            .Named( "lambda" )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (parameters, body) = parts;

                try
                {
                    return Lambda( body, parameters );
                }
                finally
                {
                    ctx.ExitScope();
                }
            } );

        static Parser<ParameterExpression[]> Parameters( Parser<Expression> typeConstant )
        {
            return ZeroOrOne(
                    Separated(
                        Terms.Char( ',' ),
                        typeConstant.And( Terms.Identifier().ElseInvalidIdentifier() )
                    )
                )
                .Named( "parameters" )
                .Then( static ( ctx, parts ) =>
                {
                    var (scope, resolver) = ctx;
                    scope.EnterScope( FrameType.Method );

                    if ( parts == null )
                        return [];

                    return parts.Select( p =>
                    {
                        var (typeName, paramName) = p;

                        var type = resolver.ResolveType( typeName.ToString() )
                                   ?? throw new InvalidOperationException( $"Unknown type: {typeName}." );

                        var name = paramName.ToString()!;
                        var parameter = Parameter( type, name );

                        scope.Variables.Add( name, parameter );

                        return parameter;

                    } ).ToArray();
                } );
        }
    }

    private static Parser<Expression> LambdaInvokeParser( Expression targetExpression, Parser<Expression> expression )
    {
        return Between(
                Terms.Char( '(' ),
                ArgsParser( expression ),
                Terms.Char( ')' )
            )
            .Named( "invoke" )
            .Then<Expression>( args => Invoke( targetExpression, args )
        );
    }
}

