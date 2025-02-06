using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

internal class WhitespaceOrNewLineOrCommentParser : Parser<TextSpan>
{
    public override bool Parse( ParseContext context, ref ParseResult<TextSpan> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        while ( true )
        {
            if ( scanner.SkipWhiteSpaceOrNewLine() )
                continue;

            // Check for trailing comments

            if ( !cursor.Match( '/' ) || cursor.PeekNext() != '/' )
            {
                context.ExitParser( this );
                return false;
            }

            cursor.Advance( 2 );
            while ( !cursor.Eof && !Character.IsNewLine( cursor.Current ) )
            {
                cursor.Advance();
            }
        }
    }
}

public static partial class XsParsers
{
    public static Parser<TextSpan> WhitespaceOrNewLineOrComment()
    {
        return new WhitespaceOrNewLineOrCommentParser();
    }
}

