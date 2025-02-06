using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public static class ParseContextExtensions
{
    public static bool StartsWith( this ParseContext context, ReadOnlySpan<char> span, bool allowWhitespace = true )
    {
        var cursor = context.Scanner.Cursor;
        var start = cursor.Position;

        if ( allowWhitespace )
            context.SkipWhiteSpace();

        var result = cursor.Span.StartsWith( span );

        cursor.ResetPosition( start );
        return result;
    }
}
