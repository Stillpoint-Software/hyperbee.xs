﻿using System.Linq.Expressions;
using System.Text;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

internal class RuntimeTypeParser : Parser<Type>
{
    private readonly bool _backtrack;

    public RuntimeTypeParser( bool backtrack )
    {
        _backtrack = backtrack;
    }

    public override bool Parse( ParseContext context, ref ParseResult<Type> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;
        var backtrack = _backtrack;

        var typeBuilder = new StringBuilder();
        var positions = backtrack ? new Stack<TextPosition>() : null;

        var start = cursor.Position;
        var position = start;

        scanner.SkipWhiteSpaceOrNewLine();

        // get dot-separated type-name

        while ( scanner.ReadIdentifier( out var segment ) )
        {
            if ( typeBuilder.Length > 0 )
                typeBuilder.Append( '.' );

            typeBuilder.Append( segment );

            if ( _backtrack )
            {
                positions!.Push( position );
                position = cursor.Position;
            }

            if ( !scanner.ReadChar( '.' ) )
                break;

            scanner.SkipWhiteSpaceOrNewLine();
        }

        // get any generic argument types

        var genericArgs = new List<Type>();

        if ( scanner.ReadChar( '<' ) )
        {
            scanner.SkipWhiteSpaceOrNewLine();
            backtrack = false; // disable backtrack if generic arguments found

            do
            {
                var genericArgResult = new ParseResult<Type>();

                if ( XsParsers.RuntimeType( backtrack: false ).Parse( context, ref genericArgResult ) ) // generic arguments cannot be backtracked
                {
                    genericArgs.Add( genericArgResult.Value );
                }
                else
                {
                    cursor.ResetPosition( start );
                    context.ExitParser( this );
                    return false;
                }

                scanner.SkipWhiteSpaceOrNewLine();

            } while ( scanner.ReadChar( ',' ) );

            if ( !scanner.ReadChar( '>' ) )
            {
                cursor.ResetPosition( start );
                context.ExitParser( this );
                return false;
            }
        }

        // resolve the type from the type-name
        //
        // if backtrack is enabled, we will try to resolve the type from the most specific type-name
        // and incrementally remove the last segment until we find a match. this is needed because
        // the type-name may have right-side properties or methods that are not part of the type-name.

        var (_, resolver) = context;
        var typeSpan = typeBuilder.ToString();

        while ( true )
        {
            var resolvedType = resolver.ResolveType( typeSpan );

            if ( resolvedType != null )
            {
                resolvedType = genericArgs.Count > 0
                    ? resolvedType.MakeGenericType( genericArgs.ToArray() )
                    : resolvedType;

                result.Set( start.Offset, cursor.Position.Offset, resolvedType );
                context.ExitParser( this );
                return true;
            }

            if ( !backtrack || positions.Count == 0 )
                break;

            // Adjust the span by removing the last segment

            cursor.ResetPosition( positions.Pop() );
            var lastDotIndex = typeSpan.LastIndexOf( '.' );

            if ( lastDotIndex == -1 )
                break;

            typeSpan = typeSpan[..lastDotIndex];
        }

        cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

internal static partial class XsParsers
{
    public static Parser<Type> RuntimeType( bool backtrack = false )
    {
        return new RuntimeTypeParser( backtrack );
    }
}
