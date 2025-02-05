using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public class ExpressionWriterContext
{
    internal readonly HashSet<string> Usings = [
        "System",
        "System.Linq.Expressions",
    ];

    internal readonly Dictionary<ParameterExpression, string> Parameters = [];
    internal readonly Dictionary<LabelTarget, string> Labels = [];

    internal readonly StringWriter ParameterOutput = new();
    internal readonly StringWriter LabelOutput = new();
    internal readonly StringWriter ExpressionOutput = new();

    internal int IndentDepth = 0;

    internal string Indention => Config.Indentation;
    internal string Prefix => Config.Prefix;
    internal string Variable => Config.Variable;

    internal IExtensionWriter[] ExtensionWriters => Config.Writers;

    internal ExpressionVisitor Visitor { get; init; }
    internal ExpressionVisitorConfig Config { get; init; }

    internal ExpressionWriterContext( ExpressionVisitorConfig config = null )
    {
        Config = config ?? new();
        Visitor = new ExpressionVisitor( this );
    }

    public static void WriteTo( Expression expression, StringWriter output, ExpressionVisitorConfig config = null )
    {
        var context = new ExpressionWriterContext( config );

        var writer = context
            .GetWriter()
            .WriteExpression( expression );

        var usings = string.Join( '\n', context.Usings.Select( u => $"using {u};" ) );
        var parameterOutput = context.ParameterOutput.ToString();
        var labelOutput = context.LabelOutput.ToString();

        output.WriteLine( usings );
        output.WriteLine();

        if ( !string.IsNullOrEmpty( parameterOutput ) )
            output.WriteLine( parameterOutput );

        if ( !string.IsNullOrEmpty( labelOutput ) )
            output.WriteLine( labelOutput );

        output.Write( $"var {context.Variable} = {context.ExpressionOutput};" );
    }

    public ExpressionWriter EnterExpression( string name, bool newLine = true, bool prefix = true )
    {
        var writer = new ExpressionWriter( this, ( w ) => ExitExpression( w, newLine ) );

        writer.Write( $"{(prefix ? Prefix : string.Empty)}{name}(", indent: true );

        if ( newLine )
            writer.Write( "\n" );

        writer.Indent();

        return writer;
    }

    public ExpressionWriter GetWriter()
    {
        return new ExpressionWriter( this, null );
    }

    private void ExitExpression( ExpressionWriter writer, bool newLine = true )
    {
        writer.Outdent();

        if ( newLine )
        {
            writer.Write( "\n" );
            writer.Write( ")", indent: true );
        }
        else
        {
            writer.Write( ")" );
        }
    }
}
