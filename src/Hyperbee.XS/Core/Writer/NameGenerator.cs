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
        var typeName = type.Name;

        if ( type.IsGenericType )
        {
            var backtickIndex = typeName.IndexOf( '`' );
            if ( backtickIndex > 0 )
            {
                typeName = typeName[..backtickIndex];
            }
        }

        var parts = SplitTypeNameByCasing( typeName );
        var shortParts = parts.Select( part => part.Length > 3 ? part[..3] : part );

        return string.Join( string.Empty, shortParts )
            .Insert( 0, shortParts.First()[..1].ToLowerInvariant() )
            .Remove( 1, 1 );

        static List<string> SplitTypeNameByCasing( ReadOnlySpan<char> typeName )
        {
            var parts = new List<string>();
            var start = 0;

            for ( var i = 1; i < typeName.Length; i++ )
            {
                if ( char.IsLower( typeName[i - 1] ) && char.IsUpper( typeName[i] ) )
                {
                    parts.Add( typeName[start..i].ToString() );
                    start = i;
                }
            }

            if ( start < typeName.Length )
            {
                parts.Add( typeName[start..].ToString() );
            }

            return parts;
        }
    }

}

internal static class Keywords
{
    private static readonly HashSet<string> _keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    ];

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    internal static bool IsKeyword( string name ) => _keywords.Contains( name );
}
