using System.Linq.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;

using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions.Lab;

public class RegexParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "regex";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var expression = binder.ExpressionParser;

        // Define the regex select parser
        var regexPattern = SkipWhiteSpace( new StringLiteral( '/' ) )
            .Then<Expression>( static value => Constant( value.ToString() ) );

        return
            expression
                .And( Terms.Text( "::" ).SkipAnd( regexPattern ) )
                .Then<Expression>( static parts =>
                {
                    var (regex, pattern) = parts;

                    return new RegexMatchExpression( regex, pattern );
                } )
                .Named( "regex" );
    }

    public bool CanWrite( Expression node )
    {
        return node is RegexMatchExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not RegexMatchExpression regexExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Xs.Extensions.Lab.ExpressionExtensions.Regex", true, false );

        writer.WriteExpression( regexExpression.InputExpression );
        writer.Write( ",\n" );
        writer.Write( regexExpression.Pattern, indent: true );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not RegexMatchExpression regexExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "regex " );
        writer.WriteExpression( ExpressionExtensions.Regex( regexExpression.InputExpression, regexExpression.Pattern ) );

        if ( regexExpression.Pattern != null )
        {
            writer.Write( "::" );
            writer.Write( regexExpression.Pattern );
        }
    }
}
