using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class RunFileCommand : Command<RunFileCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
        [Description( "File to run" )]
        [CommandArgument( 0, "<file>" )]
        public string ScriptFile { get; init; }

        [Description( "Show the expression tree instead of running" )]
        [CommandOption( "-s|--show" )]
        [DefaultValue( false )]
        public bool? Show { get; init; }
    }

    public override int Execute( [NotNull] CommandContext context, [NotNull] Settings settings )
    {
        if ( !File.Exists( settings.ScriptFile ) )
        {
            AnsiConsole.Markup( $"[red]Invalid file[/]" );
            return 1;
        }

        try
        {
            var references = AssemblyHelper.GetAssembly( settings.References );
            var script = File.ReadAllText( settings.ScriptFile );

            if ( settings.Show.Value )
            {
                var result = Script.Show( script, references );

                AnsiConsole.MarkupInterpolated( $"[green]Result:[/]\n" );
                AnsiConsole.Write( new Panel( new Text( result ) )
                {
                    Border = BoxBorder.Rounded,
                    Expand = true
                } );
            }
            else 
            {
                var result = Script.Execute( script, references );
                AnsiConsole.MarkupInterpolated( $"[green]Result:[/] {result}\n" );
            }
            return 0;
        }
        catch ( Exception ex )
        {
            AnsiConsole.MarkupInterpolated( $"[red]Error executing script: {ex.Message}[/]\n" );
            return 1;
        }
    }
}
