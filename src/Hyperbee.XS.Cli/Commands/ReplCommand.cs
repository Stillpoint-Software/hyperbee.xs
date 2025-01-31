using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class ReplCommand : Command<ReplCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
    }

    public override int Execute( [NotNull] CommandContext context, [NotNull] Settings settings )
    {
        AnsiConsole.Markup( "[yellow]Starting REPL session. Type 'exit' to quit.[/]\n" );

        while ( true )
        {
            AnsiConsole.Markup( "[cyan]> [/]" );
            var input = Console.ReadLine();

            if ( input?.ToLower() == "exit" )
            {
                return 0;
            }

            try
            {
                var result = "TODO"; //ExecuteScript( input! );
                AnsiConsole.Markup( $"[green]Result:[/] {result}\n" );
            }
            catch ( Exception ex )
            {
                AnsiConsole.Markup( $"[red]Error: {ex.Message}[/]\n" );
                return 1;
            }
        }
    }
}
