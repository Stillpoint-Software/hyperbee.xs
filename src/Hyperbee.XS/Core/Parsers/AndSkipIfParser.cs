using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public class AndSkipIfParser<T, U> : Parser<T>
{
    private readonly Parser<T> _firstParser;
    private readonly Func<ParseContext, T, bool> _condition;
    private readonly Parser<U> _trueParser;
    private readonly Parser<U> _falseParser;

    public AndSkipIfParser( Parser<T> firstParser, Func<ParseContext, T, bool> condition, Parser<U> trueParser, Parser<U> falseParser )
    {
        _firstParser = firstParser ?? throw new ArgumentNullException( nameof( firstParser ) );
        _condition = condition ?? throw new ArgumentNullException( nameof( condition ) );
        _trueParser = trueParser ?? throw new ArgumentNullException( nameof( trueParser ) );
        _falseParser = falseParser ?? throw new ArgumentNullException( nameof( falseParser ) );

        Name = $"AndSkipIf({firstParser.Name})";
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var result1 = new ParseResult<T>();

        var start = cursor.Position;

        if ( _firstParser.Parse( context, ref result1 ) )
        {
            var nextParser = _condition( context, result.Value ) ? _trueParser : _falseParser;
            var result2 = new ParseResult<U>();

            if ( nextParser.Parse( context, ref result2 ) )
            {
                result.Set( result1.Start, result2.End, result1.Value );

                context.ExitParser( this );
                return true;
            }

            cursor.ResetPosition( start );
        }

        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static Parser<T> AndSkipIf<T, U>( this Parser<T> parser, Func<ParseContext, T, bool> condition, Parser<U> trueParser, Parser<U> falseParser )
    {
        return new AndSkipIfParser<T, U>( parser, condition, trueParser, falseParser );
    }
}

