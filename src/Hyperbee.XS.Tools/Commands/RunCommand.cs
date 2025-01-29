using System.Linq.Expressions;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Hyperbee.XS;
using Spectre.Console;

namespace Hyperbee.Xs.Tools.Commands;

[Command( "run", Description = "Runs the provided script file" )]
public class RunCommand : ICommand
{
    [CommandOption( "file", 'f', Description = "The path to the script file to execute (optional, can provide script as input instead)" )]
    public string? ScriptFile { get; set; }

    [CommandOption( "script", 's', Description = "The script content to execute directly" )]
    public string? Script { get; set; }

    public async ValueTask ExecuteAsync( IConsole console )
    {
        if ( Script == null && ScriptFile == null )
        {
            AnsiConsole.Markup( "[red]Error: No script provided.[/]\n" );
            return;
        }
        else if ( Script == null && ScriptFile != null )
        {
            if ( !File.Exists( ScriptFile ) )
            {
                AnsiConsole.Markup( "[red]Error: File not found.[/]\n" );
                return;
            }
            Script = await File.ReadAllTextAsync( ScriptFile );
        }

        try
        {
            var result = ExecuteScript( Script! );

            AnsiConsole.Markup( $"[green]Result:[/] {result}\n" );
        }
        catch ( Exception ex )
        {
            AnsiConsole.Markup( $"[red]Error executing script: {ex.Message}[/]\n" );
        }
    }

    private static string ExecuteScript( string script )
    {
        var parser = new XsParser();

        var expression = parser.Parse( script );

        var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
        var lambda = Expression.Lambda( delegateType, expression );
        var compiled = lambda.Compile();
        var result = compiled.DynamicInvoke();

        return result?.ToString() ?? "null";
    }
}
