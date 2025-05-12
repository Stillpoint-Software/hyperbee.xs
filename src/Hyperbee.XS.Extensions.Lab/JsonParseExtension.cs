using System.Linq.Expressions;
using System.Text.Json;
using Hyperbee.Expressions.Lab;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;
using static Hyperbee.Expressions.Lab.ExpressionExtensions;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions.Lab;

public class JsonParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "json";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var expression = binder.ExpressionParser;
        // var element = json """{ "first": 1, "second": 2 }"""
        // var person = json<Person> """{ "name": "John", "age": 30 }"""

        var jsonPathSelect = SkipWhiteSpace( new StringLiteral( '/' ) )
            .Then<Expression>( static value => Constant( value.ToString() ) );

        return
            ZeroOrOne(
                    Between(
                        Terms.Char( '<' ),
                        XsParsers.TypeRuntime(),
                        Terms.Char( '>' )
                    )
                )
                .AndSkip( new WhiteSpaceLiteral( true ) )
                .And( expression )
                .Then<Expression>( static parts =>
                {
                    var (type, value) = parts;
                    if ( value.Type == typeof( HttpResponseMessage ) )
                        return Await( ReadJson( value, type ?? typeof( JsonElement ) ) );

                    return Expressions.Lab.ExpressionExtensions.Json( value, type );
                } )
                .And(
                    ZeroOrOne(
                        Terms.Text( "::" ).SkipAnd( jsonPathSelect )
                    )
                ).Then( static ( ctx, parts ) =>
                    {
                        var (json, select) = parts;

                        return select == null
                            ? json
                            : JsonPath( json, select );
                    }
                )
                .Named( "json" );
    }

    public bool CanWrite( Expression node )
    {
        return node is JsonExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not JsonExpression jsonExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.Lab.ExpressionExtensions.Json", true, false );

        writer.WriteExpression( jsonExpression.InputExpression );
        writer.Write( ",\n" );
        writer.WriteType( jsonExpression.Type );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not JsonExpression jsonExtension )
            return;

        using var writer = context.GetWriter();

        writer.Write( "json " );
        writer.WriteExpression( jsonExtension.InputExpression );
    }
}

