using System.Text;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

internal class TypeRuntimeParser : Parser<Type>
{
    private readonly bool _backtrack;

    public TypeRuntimeParser( bool backtrack )
    {
        _backtrack = backtrack;
        Name = "TypeRuntime";
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

        if ( scanner.Cursor.Current == ':' )
        {
            // Invalid for types to end with a colon (Identifier is being used as a label)
            cursor.ResetPosition( start );
            context.ExitParser( this );
            return false;
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

                if ( XsParsers.TypeRuntime( backtrack: false ).Parse( context, ref genericArgResult ) ) // generic arguments cannot be backtracked
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

            typeBuilder.Append( $"`{genericArgs.Count}" );
        }

        // resolve the type from the type-name
        //
        // if backtrack is enabled, we will try to resolve the type from the most specific type-name
        // and incrementally remove the last segment until we find a match. this is needed because
        // the type-name may have right-side properties or methods that are not part of the type-name.

        var (_, resolver) = context;
        var typeName = typeBuilder.ToString();

        // Try resolving the type-name directly

        var resolvedType = resolver.ResolveType( typeName );

        if ( resolvedType == null )
        {
            // Check namespaces if any

            if ( context is XsContext xsContext )
            {
                foreach ( var ns in xsContext.Namespaces )
                {
                    var qualifiedName = $"{ns}.{typeName}";
                    resolvedType = resolver.ResolveType( qualifiedName );

                    if ( resolvedType != null )
                        break;
                }
            }
        }

        // Apply fallback backtracking if enabled

        while ( resolvedType == null && backtrack && positions.Count > 0 )
        {
            cursor.ResetPosition( positions.Pop() );
            var lastDotIndex = typeName.LastIndexOf( '.' );

            if ( lastDotIndex == -1 )
                break;

            typeName = typeName[..lastDotIndex];
            resolvedType = resolver.ResolveType( typeName );
        }

        if ( resolvedType != null )
        {
            resolvedType = genericArgs.Count > 0
                ? resolvedType.MakeGenericType( genericArgs.ToArray() )
                : resolvedType;

            result.Set( start.Offset, cursor.Position.Offset, resolvedType );
            context.ExitParser( this );
            return true;
        }

        cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static Parser<Type> TypeRuntime( bool backtrack = false )
    {
        return new TypeRuntimeParser( backtrack );
    }
}
