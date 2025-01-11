﻿using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    // Member Parsers

    private static Parser<Expression> IndexerAccessParser( Expression targetExpression, Parser<Expression> expression )
    {
        return Between(
                Terms.Char( '[' ),
                Separated( Terms.Char( ',' ), expression ),
                Terms.Char( ']' )
            )
            .Then<Expression>( indexes =>
            {
                var indexers = targetExpression.Type.GetProperties()
                    .Where( p => p.GetIndexParameters().Length == indexes.Count )
                    .ToArray();

                if ( indexers.Length == 0 )
                {
                    throw new InvalidOperationException(
                        $"No indexers found on type '{targetExpression.Type}' with {indexes.Count} parameters." );
                }

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
        );
    }

    private static Parser<Expression> MemberAccessParser( Expression targetExpression, Parser<Expression> expression )
    {
        return Terms.Char( '.' )
            .SkipAnd(
                Terms.Identifier()
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
                                ArgumentsParser( expression ),
                                Terms.Char( ')' )
                            )
                        )
                    )
                )
            )
            .Then<Expression>( parts =>
            {
                var (memberName, (typeArgs, args)) = parts;

                var type = ConvertToType( targetExpression );
                var name = memberName.ToString()!;

                // method

                if ( args != null )
                {
                    var method = TypeResolver.FindMethod( type, name, typeArgs, args );

                    if ( method == null )
                        throw new InvalidOperationException( $"Method '{name}' not found on type '{type}'." );

                    return method.IsStatic
                        ? Expression.Call( method, args )
                        : Expression.Call( targetExpression, method, args );
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

