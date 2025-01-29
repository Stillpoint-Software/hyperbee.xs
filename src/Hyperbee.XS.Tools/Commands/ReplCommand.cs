using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Spectre.Console;

namespace Hyperbee.Xs.Tools.Commands;

[Command( "repl", Description = "Starts an interactive REPL session" )]
public class ReplCommand : ICommand
{
    public async ValueTask ExecuteAsync( IConsole console )
    {
        AnsiConsole.Markup( "[yellow]Starting REPL session. Type 'exit' to quit.[/]\n" );

        while ( true )
        {
            AnsiConsole.Markup( "[cyan]> [/]" );
            var input = console.Input.ReadLine();

            if ( input?.ToLower() == "exit" )
            {
                break;
            }

            try
            {
                var result = ExecuteScript( input! );
                AnsiConsole.Markup( $"[green]Result:[/] {result}\n" );
            }
            catch ( Exception ex )
            {
                AnsiConsole.Markup( $"[red]Error: {ex.Message}[/]\n" );
            }
        }

        await Task.CompletedTask; // Placeholder for compile logic
    }

    private static string ExecuteScript( string scriptContents )
    {
        return $"Executed: {scriptContents}";
    }

}
