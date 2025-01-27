﻿using System.Linq.Expressions;

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

    internal char Indention => Config.Indentation;
    internal string Prefix => Config.Prefix;
    internal string Variable => Config.Variable;

    internal IExtensionWriter[] ExtensionWriters => Config.Writers;

    internal ExpressionTreeVisitor Visitor { get; init; }
    internal ExpressionTreeVisitorConfig Config { get; init; }

    internal ExpressionWriterContext( ExpressionTreeVisitorConfig config = null )
    {
        Config = config ?? new();
        Visitor = new ExpressionTreeVisitor( this );
    }

    public static void WriteTo( Expression expression, StringWriter output, ExpressionTreeVisitorConfig config = null )
    {
        var context = new ExpressionWriterContext( config );

        var writer = context
            .GetWriter()
            .WriteExpression( expression );

        var usings = string.Join( '\n', context.Usings.Select( u => $"using {u};" ) );

        output.WriteLine( usings );
        output.WriteLine();
        output.WriteLine( context.ParameterOutput );
        output.WriteLine( context.LabelOutput );
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
