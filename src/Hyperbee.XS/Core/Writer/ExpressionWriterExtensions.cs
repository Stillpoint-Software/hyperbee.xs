using System.Globalization;
using System.Linq.Expressions;

namespace Hyperbee.XS.Core.Writer;

public static class ExpressionWriterExtensions
{
    public static string ToExpressionString( this Expression expression, ExpressionVisitorConfig config = null )
    {
        using var output = new StringWriter( CultureInfo.CurrentCulture );
        ExpressionWriterContext.WriteTo( expression, output, config );
        return output.ToString();
    }

    public static void ToExpressionString( this Expression expression, StringWriter output, ExpressionVisitorConfig config = null )
    {
        ExpressionWriterContext.WriteTo( expression, output, config );
    }
}
