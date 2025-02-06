using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Writer;
using Parlot;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class StringFormatParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Literal;

    public string Key => null;

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return SkipWhiteSpace( new BacktickLiteral() )
            .Then<Expression>( static ( ctx, value ) =>
            {
                var (format, arguments) = StringFormatHelper.PrepareFormat(
                    value.ToString(),
                    ctx.Scope().Variables.EnumerateValues()
                );

                return ExpressionExtensions.StringFormat( format, arguments );

            } ).Named( "format" );
    }

    public bool CanWrite( Expression node )
    {
        return node is StringFormatExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not StringFormatExpression stringFormatExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.StringFormat", true, false );

        var variables = new ReadOnlyCollection<Expression>( [.. stringFormatExpression.Arguments] );

        writer.WriteExpression( stringFormatExpression.Format );
        writer.WriteParamExpressions( variables );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not StringFormatExpression stringFormatExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "`" );
        writer.Write( StringFormatHelper.RevertFormat( stringFormatExpression.Format, stringFormatExpression.Arguments ) );
        writer.Write( "`" );
    }
}

internal class BacktickLiteral : Parser<TextSpan>
{
    public override bool Parse( ParseContext context, ref ParseResult<TextSpan> result )
    {
        context.EnterParser( this );

        var start = context.Scanner.Cursor.Offset;

        var success = UnsafeScannerHelper.ReadQuotedString( context.Scanner, '`' );

        var end = context.Scanner.Cursor.Offset;

        if ( success )
        {
            // Remove quotes
            var decoded = Character.DecodeString( new TextSpan( context.Scanner.Buffer, start + 1, end - start - 2 ) );
            result.Set( start, end, decoded );
        }

        context.ExitParser( this );
        return success;
    }

    public class UnsafeScannerHelper
    {
        public static bool ReadQuotedString( Scanner scanner, char quoteChar )
        {
            return ReadQuotedString( scanner, quoteChar, out _ );

            // Parlot has support for quoting strings with other characters
            [UnsafeAccessor( UnsafeAccessorKind.Method, Name = nameof( Scanner.ReadQuotedString ) )]
            // ReSharper disable once LocalFunctionHidesMethod
            static extern bool ReadQuotedString( Scanner scanner, char quoteChar, out ReadOnlySpan<char> result );
        }
    }
}

internal static partial class StringFormatHelper
{
    [GeneratedRegex( @"\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)\}" )]
    private static partial Regex NamedPlaceholderRegex();

    public static (Expression template, ParameterExpression[] usedParameters) PrepareFormat( string format, IEnumerable<ParameterExpression> parameters )
    {
        var keyParameters = parameters.ToDictionary( x => x.Name );
        var usedParameters = new List<ParameterExpression>();
        var indexMap = new Dictionary<string, int>();
        var indexCounter = 0;

        string updateFormat = NamedPlaceholderRegex().Replace( format, match =>
        {
            var name = match.Groups["name"].Value;

            if ( !keyParameters.TryGetValue( name, out var parameter ) )
                throw new ArgumentException( $"The placeholder '{name}' is not a valid variable.", nameof( parameters ) );

            if ( !indexMap.TryGetValue( name, out int index ) )
            {
                index = indexCounter++;
                indexMap[name] = index;
                usedParameters.Add( parameter );
            }

            return $"{{{index}}}";
        } );

        return (Constant( updateFormat ), [.. usedParameters]);
    }

    public static string RevertFormat( Expression format, IReadOnlyList<Expression> parameters )
    {
        var formatString = (string) ((ConstantExpression) format).Value;
        var args = parameters.OfType<ParameterExpression>().Select( x => x.Name ).ToArray();

        // convert a string from "Hello {name}" to "Hello {0}"
        for ( var i = 0; i < args.Length; i++ )
        {
            formatString = formatString.Replace( $"{{{i}}}", $"{{{args[i]}}}" );
        }

        return formatString;
    }
}
