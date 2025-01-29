using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Spectre.Console;

namespace Hyperbee.Xs.Tools.Commands;

[Command( "compile", Description = "Compiles the provided script file to an assembly" )]
public class CompileCommand : ICommand
{
    [CommandParameter( 0, Description = "The path to the script file to compile" )]
    public required string ScriptFile { get; set; }

    [CommandOption( "output", Description = "The output path for the compiled assembly" )]
    public required string OutputPath { get; set; }

    public async ValueTask ExecuteAsync( IConsole console )
    {
        if ( !File.Exists( ScriptFile ) )
        {
            AnsiConsole.Markup( "[red]Error: File not found.[/]\n" );
            return;
        }

        try
        {
            AnsiConsole.Markup( $"[blue]Compiling '{ScriptFile}' to assembly at '{OutputPath}'...[/]\n" );
            await Task.CompletedTask; // Placeholder
        }
        catch ( Exception ex )
        {
            AnsiConsole.Markup( $"[red]Error compiling script: {ex.Message}[/]\n" );
        }
    }
}
