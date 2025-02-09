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

    public override string Message => $"{base.Message} {Buffer.GetLine( Line, Column, true )}";
}
