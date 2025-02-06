using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Core.Parsers;

public sealed class CharQuotedLiteral : Parser<char>
{
    private readonly StringLiteralQuotes _quotes;

    public CharQuotedLiteral( StringLiteralQuotes quotes )
    {
        _quotes = quotes;
        Name = "CharLiteral";
    }

    public override bool Parse( ParseContext context, ref ParseResult<char> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var start = scanner.Cursor.Position;

        var success = _quotes switch
        {
            StringLiteralQuotes.Single => scanner.ReadSingleQuotedString(),
            StringLiteralQuotes.Double => scanner.ReadDoubleQuotedString(),
            _ => false
        };

        var end = scanner.Cursor.Offset;

        if ( success )
        {
            var decoded = Character.DecodeString( new TextSpan( scanner.Buffer, start.Offset + 1, end - start.Offset - 2 ) );

            if ( decoded.Length == 1 )
            {
                result.Set( start.Offset, end, decoded.Span[0] );
                context.ExitParser( this );
                return true;
            }
        }

        scanner.Cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static Parser<char> CharQuoted( this TermBuilder terms, StringLiteralQuotes quotes )
    {
        return SkipWhiteSpace( new CharQuotedLiteral( quotes ) );
    }
}

