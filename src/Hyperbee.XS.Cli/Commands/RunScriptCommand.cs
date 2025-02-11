using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class RunScriptCommand : Command<RunScriptCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
        [CommandArgument( 0, "[script]" )]
        public string Script { get; init; }
    }

    public override int Execute( [NotNull] CommandContext context, [NotNull] Settings settings )
    {
        var script = settings.Script;

        try
        {
            var show = false;

            if ( string.IsNullOrWhiteSpace( script ) )
            {
                AnsiConsole.Markup( $"[teal]script:[/] (type [green]\"run\"[/] to execute or [green]\"show\"[/] to see C# expression tree) \n" );
                var prompt = new TextPrompt<string>( "[cyan]>[/]" );
                while ( true )
                {
                    var line = AnsiConsole.Prompt( prompt );
                    if ( line == "run" )
                    {
                        break;
                    }
                    if ( line == "show" )
                    {
                        show = true;
                        break;
                    }

                    script += line + "\n";
                }
            }

            if ( show )
            {
                var result = Script.Show( script, settings.CreateConfig() );

                AnsiConsole.MarkupInterpolated( $"[green]Result:[/]\n" );
                AnsiConsole.Write( new Panel( new Text( result ) )
                {
                    Border = BoxBorder.Rounded,
                    Expand = true
                } );
            }
            else
            {
                var result = Script.Execute( script, settings.CreateConfig() );

                AnsiConsole.MarkupInterpolated( $"[green]Result:[/] {result}\n" );
            }
        }
        catch ( Exception ex )
        {
            AnsiConsole.MarkupInterpolated( $"[red]Error executing script: {ex.Message}[/]\n" );
            return 1;
        }

        return 0;
    }
}
