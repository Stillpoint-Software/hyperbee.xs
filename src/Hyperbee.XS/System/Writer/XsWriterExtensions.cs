using System.Globalization;
using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public static class XsWriterExtensions
{
    public static string ToXS( this Expression expression, XsVisitorConfig config = null )
    {
        using var output = new StringWriter( CultureInfo.CurrentCulture );
        XsWriterContext.WriteTo( expression, output, config );
        return output.ToString();
    }

    public static void ToXS( this Expression expression, StringWriter output, XsVisitorConfig config = null )
    {
        XsWriterContext.WriteTo( expression, output, config );
    }
}
