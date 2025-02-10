namespace Hyperbee.XS.Core;

public static class BufferHelper
{
    public static ReadOnlySpan<char> GetLine( ReadOnlySpan<char> buffer, int offset )
    {
        return GetLine( buffer, offset, out _ );
    }

    public static ReadOnlySpan<char> GetLine( ReadOnlySpan<char> buffer, int offset, out int caret )
    {
        caret = 0;

        if ( (uint) offset >= (uint) buffer.Length )
        {
            return ReadOnlySpan<char>.Empty;
        }

        // If offset is on a newline character, move left
        if ( buffer[offset] == '\r' || buffer[offset] == '\n' )
        {
            if ( offset > 0 && (buffer[offset - 1] == '\r' || buffer[offset - 1] == '\n') )
            {
                return ReadOnlySpan<char>.Empty;
            }

            offset--;
        }

        // Find the left boundary
        var start = offset;
        while ( start > 0 && buffer[start - 1] != '\r' && buffer[start - 1] != '\n' )
        {
            start--;
        }

        // Find the right boundary
        int end = offset;
        while ( end < buffer.Length && buffer[end] != '\r' && buffer[end] != '\n' )
        {
            end++;
        }

        // Compute caret position relative to the extracted line
        caret = offset - start;

        return buffer.Slice( start, end - start );
    }
}
