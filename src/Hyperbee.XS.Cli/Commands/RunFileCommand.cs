using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class RunFileCommand : Command<RunFileCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
        [Description( "File" )]
        [CommandArgument( 0, "[file]" )]
        public string ScriptFile { get; init; }
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
            var result = Script.Execute( script, references );

            AnsiConsole.MarkupInterpolated( $"[green]Result:[/] {result}\n" );
        }
        catch ( Exception ex )
        {
            AnsiConsole.MarkupInterpolated( $"[red]Error executing script: {ex.Message}[/]\n" );
            return 1;
        }

        return 0;
    }
}
