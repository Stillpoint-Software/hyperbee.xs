using Hyperbee.XS.Core;
using Parlot;

namespace Hyperbee.XS;

public class SyntaxException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }

    public string Buffer { get; }

    public SyntaxException( string message, Cursor cursor = null )
        : base( message )
    {
        if ( cursor == null )
            return;

        Line = cursor.Position.Line;
        Column = cursor.Position.Column;
        Offset = cursor.Position.Offset;
        Buffer = cursor.Buffer;
    }

    public override string Message
    {
        get
        {
            var sourceLine = BufferHelper.GetLine( Buffer, Offset, out var caret );
            var caretLine = new string( ' ', caret ) + "^";

            return $"{base.Message}\n({Line} {Column})\n{sourceLine}\n{caretLine}";
        }
    }
}
