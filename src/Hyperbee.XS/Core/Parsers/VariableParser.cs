using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

internal class VariableParser : Parser<Expression>
{
    public VariableParser()
    {
        Name = "Variable";
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var start = cursor.Position;
        scanner.SkipWhiteSpaceOrNewLine();

        if ( scanner.ReadIdentifier( out var identifier ) )
        {
            var (scope, _) = context;
            var variables = scope.Variables;

            if ( variables.TryGetValue( identifier.ToString(), out var variable ) )
            {
                result.Set( start.Offset, cursor.Position.Offset, variable );
                context.ExitParser( this );
                return true;
            }
        }

        cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static Parser<Expression> Variable()
    {
        return new VariableParser();
    }
}

