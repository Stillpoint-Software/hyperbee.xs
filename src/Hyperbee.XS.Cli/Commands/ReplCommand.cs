using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using NuGet.Common;
using Spectre.Console;
using Spectre.Console.Cli;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.Xs.Cli.Commands;

internal class ReplCommand : Command<ReplCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
    }

    public override int Execute( [NotNull] CommandContext context, [NotNull] Settings settings )
    {
        AnsiConsole.Markup( "[yellow]Starting REPL session. Type [green]\"run\"[/] to run the current block, [green]\"exit\"[/] to quit, [green]\"print\"[/] to see variables.[/]\n" );

        var xsConfig = settings.CreateConfig();

        var prompt = new TextPrompt<string>( "[cyan]>[/]" );

        var values = new Dictionary<string, object>();
        var scope = new ParseScope();
        scope.EnterScope( FrameType.Method );

        while ( true )
        {
            var script = string.Empty;
            var run = false;

            try
            {
                while ( true )
                {
                    var line = AnsiConsole.Prompt( prompt );

                    if ( line == "exit" )
                    {
                        return 0;
                    }

                    if ( line == "run" )
                    {
                        run = true;
                        break;
                    }

                    if ( line == "print" )
                    {
                        var table = new Table()
                            .AddColumn( "Name" )
                            .AddColumn( "Value" );

                        foreach ( var (name, value) in values )
                        {
                            table.AddRow( name, value?.ToString() ?? string.Empty );
                        }

                        AnsiConsole.Write( table );

                        break;
                    }

                    script += line + "\n";
                }

                if ( run )
                {
                    var parser = new XsParser( xsConfig );

                    var result = parser
                        .ParseWithState( script, scope, values )
                        .InvokeWithState( scope )?.ToString() ?? "null";

                    AnsiConsole.MarkupInterpolated( $"[green]Result:[/]\n" );
                    AnsiConsole.Write( new Panel( new Text( result ) )
                    {
                        Border = BoxBorder.Rounded,
                        Expand = true
                    } );
                }
            }
            catch ( Exception ex )
            {
                AnsiConsole.Markup( $"[red]Error: {ex.Message}[/]\n" );
            }
        }
    }
}


internal class SpectreConsoleLogger : ILogger
{
    private static readonly Dictionary<LogLevel, string> LogColors = new()
    {
        { LogLevel.Debug, "grey" },
        { LogLevel.Information, "white" },
        { LogLevel.Warning, "yellow" },
        { LogLevel.Error, "red" },
        { LogLevel.Verbose, "blue" },
        { LogLevel.Minimal, "green" }
    };

    public void Log( LogLevel level, string data ) => AnsiConsole.MarkupInterpolated( $"[{LogColors[level]}]{data}[/]\n" );
    public void Log( ILogMessage message ) => Log( message.Level, message.Message );
    public void LogDebug( string data ) => Log( LogLevel.Debug, data );
    public void LogError( string data ) => Log( LogLevel.Error, data );
    public void LogInformation( string data ) => Log( LogLevel.Information, data );
    public void LogInformationSummary( string data ) => Log( LogLevel.Information, data );
    public void LogMinimal( string data ) => Log( LogLevel.Information, data );
    public void LogVerbose( string data ) => Log( LogLevel.Verbose, data );
    public void LogWarning( string data ) => Log( LogLevel.Warning, data );
    public Task LogAsync( LogLevel level, string data )
    {
        Log( level, data );
        return Task.CompletedTask;
    }
    public Task LogAsync( ILogMessage message ) => LogAsync( message.Level, message.Message );

}
