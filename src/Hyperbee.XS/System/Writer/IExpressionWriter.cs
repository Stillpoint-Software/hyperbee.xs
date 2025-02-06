using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public interface IExpressionWriter
{
    bool CanWrite( Expression node );
    void WriteExpression( Expression node, ExpressionWriterContext context );
}
