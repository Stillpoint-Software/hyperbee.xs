using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

// BreakOn allows parsing a sequence of elements until a specific
// break condition is encountered.
// 
// It wraps an inner parser and a break condition parser. 
// If the break condition matches at the current position in the input, 
// parsing halts without consuming the break input.
// 
// This is particularly useful for scenarios like parsing statements 
// in a block or case clauses in a switch, where the end of the sequence 
// is indicated by specific keywords or symbols.
//
// Example Usage:
// 
// ZeroOrMany(
//   BreakOn( CaseUntil(), statement )
// )
//
// In this example:
// - `statement` is parsed repeatedly.
// - Parsing stops when `CaseUntil()` matches.

public class BreakOnParser<U, T> : Parser<T>
{
    private readonly Parser<T> _innerParser;
    private readonly Parser<U> _stoppingCondition;

    public BreakOnParser( Parser<U> stoppingCondition, Parser<T> innerParser )
    {
        _innerParser = innerParser ?? throw new ArgumentNullException( nameof( innerParser ) );
        _stoppingCondition = stoppingCondition ?? throw new ArgumentNullException( nameof( stoppingCondition ) );

        Name = $"BreakOn({innerParser.Name})";
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var cursor = context.Scanner.Cursor;
        var current = cursor.Position;

        var stoppingCheck = new ParseResult<U>();
        if ( _stoppingCondition.Parse( context, ref stoppingCheck ) )
        {
            cursor.ResetPosition( current );
            context.ExitParser( this );
            return false;
        }

        var success = _innerParser.Parse( context, ref result );
        context.ExitParser( this );
        return success;
    }
}

public static partial class XsParsers
{
    public static Parser<T> BreakOn<U, T>( Parser<U> stoppingCondition, Parser<T> parser )
    {
        return new BreakOnParser<U, T>( stoppingCondition, parser );
    }
}
