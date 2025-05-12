using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    public const string InvalidSyntaxMessage = "Invalid syntax.";
    public const string InvalidTerminationMessage = "Invalid termination, missing ';'.";
    public const string InvalidTypeMessage = "Invalid type.";
    public const string InvalidExpressionMessage = "Invalid expression.";
    public const string InvalidStatementMessage = "Invalid statement.";
    public const string InvalidIdentifier = "Missing identifier.";

    internal static Parser<char> OpenParen = Terms.Char( '(' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> CloseParen = Terms.Char( ')' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> OpenBrace = Terms.Char( '{' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> CloseBrace = Terms.Char( '}' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> OpenBracket = Terms.Char( '[' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> CloseBracket = Terms.Char( ']' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> Assignment = Terms.Char( '=' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> Delimiter = Terms.Char( ',' ).ElseError( InvalidSyntaxMessage );
    internal static Parser<char> Terminator = Terms.Char( ';' ).ElseError( InvalidTerminationMessage );
    internal static Parser<char> Colon = Terms.Char( ':' ).ElseError( InvalidSyntaxMessage );
}

public static class XsParserExtensions
{
    internal static Parser<Expression> ElseInvalidType( this Parser<Expression> parser ) => parser.ElseError( XsParser.InvalidTypeMessage );
    internal static Parser<Expression> ElseInvalidExpression( this Parser<Expression> parser ) => parser.ElseError( XsParser.InvalidExpressionMessage );
    internal static Parser<Expression> ElseInvalidStatement( this Parser<Expression> parser ) => parser.ElseError( XsParser.InvalidStatementMessage );
    internal static Parser<TextSpan> ElseInvalidIdentifier( this Parser<TextSpan> parser ) => parser.ElseError( XsParser.InvalidIdentifier );
}
