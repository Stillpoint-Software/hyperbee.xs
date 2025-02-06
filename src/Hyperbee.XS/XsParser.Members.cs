using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            .Then<Expression>( indexes =>
            {
                var indexers = targetExpression.Type.GetProperties()
                    .Where( p => p.GetIndexParameters().Length == indexes.Count )
                    .ToArray();

                if ( indexers.Length != 0 )
                {
                    // Find the best match based on parameter types
                    var indexer = indexers.FirstOrDefault( p =>
                        p.GetIndexParameters()
                            .Select( param => param.ParameterType )
                            .SequenceEqual( indexes.Select( i => i.Type ) ) );

                    if ( indexer == null )
                    {
                        throw new InvalidOperationException(
                            $"No matching indexer found on type '{targetExpression.Type}' with parameter types: " +
                            $"{string.Join( ", ", indexes.Select( i => i.Type.Name ) )}." );
                    }

                    return Expression.Property( targetExpression, indexer, indexes.ToArray() );
                }

                return Expression.ArrayAccess( targetExpression, indexes );
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
                            Between(
                                Terms.Char( '(' ),
                                ArgsParser( expression ),
                                CloseParen
                            )
                        )
                    )
                )
            )
            .Then<Expression>( ( ctx, parts ) =>
            {
                var (memberName, (typeArgs, args)) = parts;

                var type = TypeOf( targetExpression );
                var name = memberName.ToString()!;

                // method

                if ( args != null )
                {
                    var (_, resolver) = ctx;
                    var method = resolver.ResolveMethod( type, name, typeArgs, args );

                    if ( method == null )
                        throw new InvalidOperationException( $"Method '{name}' not found on type '{type}'." );

                    var extension = method.IsDefined( typeof( ExtensionAttribute ), false );
                    var arguments = extension ? new[] { targetExpression }.Concat( args ) : args;

                    return method.IsStatic
                        ? Expression.Call( method, arguments )
                        : Expression.Call( targetExpression, method, arguments );
                }

                // property or field

                const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                var member = type.GetMember( name, BindingAttr ).FirstOrDefault();

                if ( member == null )
                    throw new InvalidOperationException( $"Member '{name}' not found on type '{type}'." );

                return member switch
                {
                    PropertyInfo property => Expression.Property( targetExpression, property ),
                    FieldInfo field => Expression.Field( targetExpression, field ),
                    _ => throw new InvalidOperationException( $"Member '{name}' is not a property or field." )
                };
            } );
    }
}

