using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public class NamespaceIdentifierParser : Parser<TextSpan>
{
    public override bool Parse( ParseContext context, ref ParseResult<TextSpan> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;
        var start = cursor.Position;

        scanner.SkipWhiteSpaceOrNewLine();

        var spanStart = cursor.Offset;
        var lastValidPosition = spanStart;

        bool hasIdentifier = false;

        while ( !cursor.Eof )
        {
            var checkpoint = cursor.Position;

            if ( hasIdentifier )
            {
                if ( cursor.Current != '.' )
                {
                    cursor.ResetPosition( checkpoint );
                    break;
                }

                cursor.Advance(); // Consume '.'
            }

            if ( !scanner.ReadIdentifier() )
            {
                cursor.ResetPosition( start );
                context.ExitParser( this );
                return false;
            }

            hasIdentifier = true;
            lastValidPosition = cursor.Offset;
        }

        if ( !hasIdentifier )
        {
            cursor.ResetPosition( start );
            context.ExitParser( this );
            return false;
        }

        result.Set( spanStart, lastValidPosition, new TextSpan( scanner.Buffer, spanStart, lastValidPosition - spanStart ) );

        context.ExitParser( this );
        return true;
    }
}

public static partial class XsParsers
{
    public static Parser<TextSpan> NamespaceIdentifier( this TermBuilder terms )
    {
        return new NamespaceIdentifierParser();
    }
}
