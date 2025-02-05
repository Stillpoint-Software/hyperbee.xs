using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public class XsWriterContext
{
    internal readonly Dictionary<ParameterExpression, string> Parameters = [];

    internal readonly StringWriter ExpressionOutput;

    internal int IndentDepth = 0;
    internal bool SkipTerminated = false;
    internal bool ForceBlock = false;

    internal string Indention => Config.Indentation;

    internal IXsWriter[] ExtensionWriters => Config.Writers;

    internal XsVisitor Visitor { get; init; }
    internal XsVisitorConfig Config { get; init; }

    internal XsWriterContext( StringWriter output = null, XsVisitorConfig config = null )
    {
        ExpressionOutput = output ?? new();
        Config = config ?? new();
        Visitor = new XsVisitor( this );
    }

    public static void WriteTo( Expression expression, StringWriter output, XsVisitorConfig config = null )
    {
        var context = new XsWriterContext( output, config );

        var writer = context.GetWriter();

        if ( expression is BlockExpression block )
        {
            foreach ( var e in block.Expressions )
            {
                writer.WriteExpression( e );
                writer.WriteTerminated();
            }
        }
        else
        {
            writer.WriteExpression( expression );
            writer.WriteTerminated();
        }
    }

    public XsWriter GetWriter()
    {
        return new XsWriter( this, null );
    }
}
