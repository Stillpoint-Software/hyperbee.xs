using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class UsingParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "using";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            Between(
                Terms.Char( '(' ),
                Terms.Text( "var" )
                    .SkipAnd( Terms.Identifier() )
                    .AndSkip( Terms.Text( "=" ) )
                    .And( expression ),
                Terms.Char( ')' )
            ).Then( static ( ctx, parts ) =>
            {
                var (variableIdentifier, disposable) = parts;

                var variableName = variableIdentifier.ToString()!;
                var variable = Variable(
                    disposable.Type,
                    variableName );

                ctx.Scope().Variables.Add( variableName, variable );

                return (variable, disposable);
            } )
            .And( statement )
            .Then<Expression>( static ( _, parts ) =>
            {
                var ((disposeVariable, disposable), body) = parts;

                return ExpressionExtensions.Using( disposeVariable, disposable, body );
            } ).Named( "using" );
    }

    public bool CanWrite( Expression node )
    {
        return node is UsingExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not UsingExpression usingExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.Using", true, false );

        writer.WriteParameter( usingExpression.DisposeVariable );
        writer.Write( ",\n" );
        writer.WriteExpression( usingExpression.Disposable );
        writer.Write( ",\n" );
        writer.WriteExpression( usingExpression.Body );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not UsingExpression usingExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "using (" );
        writer.WriteParameter( usingExpression.DisposeVariable );
        writer.Write( " = " );
        writer.WriteExpression( usingExpression.Disposable );
        writer.Write( ")\n" );
        writer.WriteExpression( usingExpression.Body );
    }
}
