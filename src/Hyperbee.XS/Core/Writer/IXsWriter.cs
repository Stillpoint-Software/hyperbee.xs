using System.Linq.Expressions;

namespace Hyperbee.XS.Core.Writer;

public interface IXsWriter
{
    bool CanWrite( Expression node );
    void WriteExpression( Expression node, XsWriterContext context );
}
