namespace Hyperbee.XS;

public static class BufferExtensions
{
    public static string GetLine( this string buffer, int line, int column, bool showCaret = false )
    {
        if ( string.IsNullOrEmpty( buffer ) || line <= 0 )
            return string.Empty;

        var currentLine = 1;
        var lineStart = 0;
        var span = buffer.AsSpan();

        for ( var i = 0; i < span.Length; i++ )
        {
            if ( span[i] != '\r' && span[i] != '\n' )
                continue;

            // Handle line endings (normalize for \r\n, \n, or \r)
            if ( span[i] == '\r' && i + 1 < span.Length && span[i + 1] == '\n' )
                i++;

            currentLine++;

            if ( currentLine > line )
                return FormatLine( span[lineStart..i], line, column, showCaret );

            lineStart = i + 1;
        }

        return currentLine != line
            ? string.Empty // Line number is out of range
            : FormatLine( span[lineStart..], line, column, showCaret );
    }

    private static string FormatLine( ReadOnlySpan<char> lineSpan, int line, int column, bool showCaret )
    {
        var lineText = lineSpan.ToString();

        if ( !showCaret || column <= 0 || column > lineSpan.Length + 1 )
            return lineText;

        var caretLine = new string( ' ', column - 1 ) + "^";
        return $"(Line: {line}, Column: {column})\n{lineText}\n{caretLine}";
    }
}
