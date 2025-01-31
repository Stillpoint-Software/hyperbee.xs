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
            var references = AssemblyHelper.GetAssembly( settings.References );
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
        }
        catch ( Exception ex )
        {
            AnsiConsole.MarkupInterpolated( $"[red]Error executing script: {ex.Message}[/]\n" );
            return 1;
        }




        return 0;
    }
}



//[Command( "run script", Description = "Runs the provided script directly from the input" )]
//public class RunScriptCommand : ICommand
//{
//    [CommandOption( 
//        "references", 'r', 
//        Description = "List of assembly paths or names to reference",
//        Converter = typeof( AssemblyCollectionConverter ) )]
//    public IReadOnlyCollection<Assembly> References { get; set; }

//    [CommandParameter( 0, Description = "The script content to execute directly",IsRequired = false )]
//    public string Script { get; set; }

//    public ValueTask ExecuteAsync( IConsole console )
//    {
//        try
//        {
//            var show = false;
//            if ( string.IsNullOrWhiteSpace( Script ) )
//            {
//                AnsiConsole.Markup( $"[teal]script:[/] (type [green]\"run\"[/] to execute or [green]\"show\"[/] to see C# expression tree) \n" );
//                var prompt = new TextPrompt<string>( "[olive]>[/]" );
//                while ( true )
//                {
//                    var line = AnsiConsole.Prompt( prompt );
//                    if ( line == "run" )
//                    {
//                        break;
//                    }
//                    if ( line == "show" )
//                    {
//                        show = true;
//                        break;
//                    }
//                    Script += line + "\n";
//                }
//            }

//            var result = show 
//                ? ScriptRunner.Show( Script, References ) 
//                : ScriptRunner.Execute( Script, References );

//            AnsiConsole.MarkupInterpolated( $"[green]Result:[/]\n\n{result}\n" );
//        }
//        catch ( Exception ex )
//        {
//            AnsiConsole.MarkupInterpolated( $"[red]Error executing script: {ex.Message}[/]\n" );
//        }

//        return default;
//    }


//}
