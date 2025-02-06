using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public class LeftAssociativeParser<T> : Parser<T>
{
    private readonly Parser<T> _leftParser;
    private readonly Func<T, Parser<T>>[] _rightFactories;

    // A parser designed to handle left-associative patterns where the right parser 
    // depends dynamically on the result of the left parser. This allows for parsing 
    // constructs such as method chains, property chains, or other left-associative 
    // grammars where subsequent elements are based on the previously parsed result.
    // 
    // Factories:
    // Factories are functions that take the current "left" result as input and return a 
    // parser for the "right" part. Each factory dynamically creates a parser for the 
    // subsequent element in the chain based on the previously parsed value. The result 
    // of this right parser becomes the new "left," enabling chaining.
    // 
    // Example Usage:
    // 
    // var primaryExpression = baseExpression.LeftAssociative(
    //     left => MemberAccessParser(left, expression),
    //     left => LambdaInvokeParser(left, expression),
    //     left => IndexerAccessParser(left, expression)
    // );
    // 
    // In this example:
    // - `baseExpression` is the initial parser for the left-hand side of the expression.
    // - `MemberAccessParser`, `LambdaInvokeParser`, and `IndexerAccessParser` are factories 
    //   that generate parsers for accessing members, invoking lambdas, or indexing into 
    //   collections respectively. Each factory takes the current left-hand expression 
    //   and returns a parser for the right-hand operation.
    // 
    // The parser processes chains like `obj.Property.Method().Index[0]` by iteratively 
    // parsing each element in the chain and updating the left-hand result at each step.

    public LeftAssociativeParser( Parser<T> leftParser, params Func<T, Parser<T>>[] rightFactories )
    {
        _leftParser = leftParser ?? throw new ArgumentNullException( nameof( leftParser ) );
        _rightFactories = rightFactories ?? throw new ArgumentNullException( nameof( rightFactories ) );
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        if ( !_leftParser.Parse( context, ref result ) )
        {
            context.ExitParser( this );
            return false;
        }

        T leftResult = result.Value;
        var start = result.Start;

        while ( true )
        {
            bool matched = false;

            foreach ( var factory in _rightFactories )
            {
                var rightParser = factory( leftResult );
                var rightResult = new ParseResult<T>();

                if ( !rightParser.Parse( context, ref rightResult ) )
                    continue;

                leftResult = rightResult.Value;
                matched = true;
                break;
            }

            if ( !matched )
                break;
        }

        result.Set( start, context.Scanner.Cursor.Position.Offset, leftResult );
        context.ExitParser( this );
        return true;
    }
}

public static partial class XsParsers
{
    public static LeftAssociativeParser<T> LeftAssociative<T>(
        this Parser<T> leftParser,
        params Func<T, Parser<T>>[] rightFactories )
    {
        return new LeftAssociativeParser<T>( leftParser, rightFactories );
    }
}
