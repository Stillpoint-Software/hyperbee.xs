using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public class RawStringParser : Parser<TextSpan>
{
    private enum ParserState
    {
        Initial,
        BeginContent,
        Content,
        EndContent
    }

    public override bool Parse( ParseContext context, ref ParseResult<TextSpan> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;
        var start = cursor.Position;
        TextPosition begin = default;

        scanner.SkipWhiteSpaceOrNewLine();

        var state = ParserState.Initial;
        var quoteCount = 0;
        var requiredQuoteCount = 0;

        while ( true )
        {
            var current = cursor.Current;

            switch ( state )
            {
                case ParserState.Initial:
                    if ( current == '"' )
                    {
                        quoteCount++;
                    }
                    else if ( quoteCount >= 3 )
                    {
                        state = ParserState.BeginContent;
                        requiredQuoteCount = quoteCount;
                        begin = scanner.Cursor.Position;
                        continue;
                    }
                    else
                    {
                        scanner.Cursor.ResetPosition( start );
                        context.ExitParser( this );
                        return false;
                    }
                    break;

                case ParserState.BeginContent:
                    state = ParserState.Content;
                    quoteCount = 0;
                    break;

                case ParserState.Content:
                    if ( current == '"' )
                    {
                        quoteCount++;
                        if ( quoteCount == requiredQuoteCount )
                        {
                            state = ParserState.EndContent;
                        }
                    }
                    else
                    {
                        quoteCount = 0;
                    }
                    break;

                case ParserState.EndContent:
                    var end = scanner.Cursor.Position.Offset;

                    var decoded = Character.DecodeString(
                        new TextSpan( scanner.Buffer, begin.Offset, end - begin.Offset - requiredQuoteCount )
                    );

                    result.Set( start.Offset, end, decoded );
                    context.ExitParser( this );

                    return true;

                default:
                    throw new ParseException( $"Invalid state for {nameof( RawStringParser )} found.", start );
            }

            cursor.Advance();

            if ( !cursor.Eof )
                continue;

            scanner.Cursor.ResetPosition( start );
            context.ExitParser( this );
            return false;
        }
    }
}
