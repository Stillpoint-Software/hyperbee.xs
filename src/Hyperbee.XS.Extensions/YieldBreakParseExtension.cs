using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
namespace Hyperbee.Xs.Extensions;

public class YieldBreakParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Terminated;
    public string Key => "halt";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Terms.Char( ';' )
            .Then<Expression>( static _ => ExpressionExtensions.YieldBreak() )
            .Named( "halt" );
    }

    public bool CanWrite( Expression node )
    {
        return node is YieldExpression { IsReturn: false };
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not YieldExpression { IsReturn: false } )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.YieldBreak", false, false );
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not YieldExpression )
            return;

        using var writer = context.GetWriter();
        writer.Write( "halt" );
    }
}
