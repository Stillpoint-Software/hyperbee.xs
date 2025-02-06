using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

public record KeywordParserPair<T>( string Key, Parser<T> Parser );

public sealed class KeywordLookupParser<T> : Parser<T>
{
    private readonly Dictionary<string, Parser<T>> _parsers = new();

    public KeywordLookupParser()
    {
        Name = "KeywordLookup";
    }

    public KeywordLookupParser<T> Add( string keyword, Parser<T> parser )
    {
        if ( string.IsNullOrWhiteSpace( keyword ) )
        {
            throw new ArgumentException( "Keyword cannot be null or whitespace.", nameof( keyword ) );
        }

        ArgumentNullException.ThrowIfNull( parser );

        _parsers[keyword] = parser;

        return this;
    }

    public KeywordLookupParser<T> Add( params KeywordParserPair<T>[] parsers )
    {
        ArgumentNullException.ThrowIfNull( parsers );

        foreach ( var (keyword, parser) in parsers )
        {
            Add( keyword, parser );
        }

        return this;
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var start = cursor.Position;

        scanner.SkipWhiteSpaceOrNewLine();

        if ( scanner.ReadIdentifier( out var identifier ) && _parsers.TryGetValue( identifier.ToString(), out var parser ) )
        {
            if ( parser.Parse( context, ref result ) )
            {
                scanner.SkipWhiteSpaceOrNewLine();
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
    public static KeywordLookupParser<T> KeywordLookup<T>()
    {
        return new KeywordLookupParser<T>();
    }

    public static KeywordLookupParser<T> KeywordLookup<T>( string name )
    {
        return new KeywordLookupParser<T> { Name = name };
    }
}
