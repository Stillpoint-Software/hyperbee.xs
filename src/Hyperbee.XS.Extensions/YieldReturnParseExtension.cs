using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;

namespace Hyperbee.Xs.Extensions;

public class YieldReturnParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "yield";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;

        return expression
            .Then<Expression>( static parts => ExpressionExtensions.YieldReturn( parts ) )
            .Named( "yield" );
    }

    public bool CanWrite( Expression node )
    {
        return node is YieldExpression { IsReturn: true };
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not YieldExpression { IsReturn: true } yieldExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.YieldReturn", true, false );

        writer.WriteExpression( yieldExpression.Value );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not YieldExpression yieldExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "yield " );
        writer.WriteExpression( yieldExpression.Value );
    }
}
