using System.Runtime.CompilerServices;

namespace Hyperbee.XS.Core.Writer;

internal static class NameGenerator
{
    internal static string GenerateUniqueName<T>( string name, Type type, Dictionary<T, string> lookup )
    {
        // Start with the parameter's name if it exists; otherwise, infer a name from the type
        var baseName = string.IsNullOrEmpty( name )
            ? InferName( type )
            : name;

        var uniqueName = baseName;
        var counter = 1;

        while ( lookup.ContainsValue( uniqueName ) || Keywords.IsKeyword( uniqueName ) )
        {
            uniqueName = $"{baseName}{counter}";
            counter++;
        }

        return uniqueName;
    }

    private static string InferName( Type type )
    {
        var typeName = type.Name.AsSpan();
        var result = new char[typeName.Length];
        var resultIndex = 0;

        var start = 0;
        var end = typeName.Length;

        if ( char.IsLower( typeName[0] ) )
        {
            result[resultIndex++] = char.ToLowerInvariant( typeName[0] );
            start = 1;
        }

        for ( var i = start + 1; i < end; i++ )
        {
            if ( typeName[i] == '`' )
            {
                end = i;
                break;
            }

            if ( char.IsLower( typeName[i - 1] ) && char.IsUpper( typeName[i] ) )
            {
                AppendTypePart( result, ref resultIndex, typeName, start, i );
                start = i;
            }
        }

        if ( start < typeName.Length )
        {
            AppendTypePart( result, ref resultIndex, typeName, start, end );
        }

        return new string( result, 0, resultIndex );

        static void AppendTypePart( char[] result, ref int resultIndex, ReadOnlySpan<char> typeName, int start, int end )
        {
            var length = end - start;
            var shortPart = length > 3
                ? typeName.Slice( start, 3 )
                : typeName.Slice( start, length );

            foreach ( var ch in shortPart )
            {
                result[resultIndex++] = ch;
            }
        }
    }
}

internal static class Keywords
{
    private static readonly HashSet<string> _keywords;

    static Keywords()
    {
        _keywords = new HashSet<string>( StringComparer.Ordinal )
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    internal static bool IsKeyword( string name ) => _keywords.Contains( name );
}
