using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public class BoundedParser<T> : Parser<T>
{
    private readonly Func<ParseContext, bool> _condition;
    private readonly Action<ParseContext> _before;
    private readonly Parser<T> _parser;
    private readonly Action<ParseContext> _after;

    public BoundedParser( Func<ParseContext, bool> condition, Action<ParseContext> before, Parser<T> parser, Action<ParseContext> after )
    {
        _condition = condition;
        _before = before;
        _parser = parser ?? throw new ArgumentNullException( nameof( parser ) );
        _after = after;
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var parseResult = new ParseResult<T>();

        var valid = _condition?.Invoke( context ) ?? true;

        if ( valid )
        {
            _before?.Invoke( context );

            var start = context.Scanner.Cursor.Position;

            if ( !_parser.Parse( context, ref parseResult ) )
            {
                context.Scanner.Cursor.ResetPosition( start );

                if ( _condition == null ) // if condition was true then always return true (this is what IfParser does)
                    valid = false;
            }
            else
            {
                result.Set( parseResult.Start, parseResult.End, parseResult.Value );
            }

            _after?.Invoke( context );
        }

        context.ExitParser( this );
        return valid;
    }
}

public static partial class XsParsers
{
    public static Parser<T> BoundedIf<T>(
        Func<ParseContext, bool> condition,
        Action<ParseContext> before,
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return new BoundedParser<T>( condition, before, parser, after );
    }

    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return new BoundedParser<T>( default, before, parser, after );
    }

    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser )
    {
        return new BoundedParser<T>( default, before, parser, default );
    }

    public static Parser<T> Bounded<T>(
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return new BoundedParser<T>( default, default, parser, after );
    }
}
