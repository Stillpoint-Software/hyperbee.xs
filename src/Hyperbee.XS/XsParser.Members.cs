using System.Linq.Expressions;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    private static Parser<Expression> IndexerAccessParser( Expression targetExpression, Parser<Expression> expression )
    {
        return If(
                ctx => ctx.StartsWith( "[" ),
                Between(
                    OpenBracket,
                    Separated( Terms.Char( ',' ), expression ),
                    CloseBracket
                )
            )
            .Then( ( ctx, indexes ) =>
            {
                var (_, resolver) = ctx;

                return resolver.RewriteIndexerExpression( targetExpression, indexes );
            }
        );
    }

    private static Parser<Expression> MemberAccessParser( Expression targetExpression, Parser<Expression> expression )
    {
        return Terms.Char( '.' )
            .SkipAnd(
                Terms.Identifier().ElseInvalidIdentifier()
                .And(
                    ZeroOrOne(
                        ZeroOrOne(
                            Between(
                                Terms.Char( '<' ),
                                TypeArgsParser(),
                                Terms.Char( '>' )
                            )
                        )
                        .And(
                            ZeroOrOne(
                                Between(
                                    Terms.Char( '(' ),
                                    ArgsParser( expression ),
                                    Terms.Char( ')' )
                                )
                            )
                        )
                    )
                )
            )
            .Then( ( ctx, parts ) =>
            {
                var (memberName, (typeArgs, args)) = parts;

                var name = memberName.ToString()!;

                var (_, resolver) = ctx;

                return resolver.RewriteMemberExpression( targetExpression, name, typeArgs, args );
            } );
    }
}
