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

                    var expression = parser.Parse( script, scope: scope );

                    var wrapExpression = WrapWithPersistentState( expression, scope.Variables, values );

                    var delegateType = expression.Type == typeof( void )
                        ? typeof( Action )
                        : typeof( Func<> ).MakeGenericType( expression.Type );

                    var lambda = Lambda( delegateType, wrapExpression );
                    var compiled = lambda.Compile();
                    var result = compiled.DynamicInvoke()?.ToString() ?? "null";

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

    private static BlockExpression WrapWithPersistentState(
        Expression userExpression,
        IDictionary<string, ParameterExpression> symbols,
        IDictionary<string, object> values )
    {
        var localVariables = new Dictionary<string, ParameterExpression>();
        var initExpressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var valuesConst = Constant( values );
        var indexerProperty = typeof( Dictionary<string, object> ).GetProperty( "Item" )!;

        foreach ( var (name, parameter) in symbols )
        {
            var local = Variable( parameter.Type, name );
            localVariables[name] = local;

            var keyExpr = Constant( name );

            initExpressions.Add(
                values.ContainsKey( name )
                    ? Assign( local, Convert( Property( valuesConst, indexerProperty, keyExpr ), parameter.Type ) )
                    : Assign( local, Default( parameter.Type ) )
            );

            var localAsObject = parameter.Type.IsValueType
                ? Convert( local, typeof( object ) )
                : (Expression) local;

            updateExpressions.Add(
                Assign( Property( valuesConst, indexerProperty, keyExpr ), localAsObject )
            );
        }

        var replacer = new ParameterReplacer( localVariables );

        // Capture the user expression result and wrap in a try-finally block.
        var tryBlock = replacer.Visit( userExpression );

        // remove variables from top level block
        if ( tryBlock is BlockExpression block )
            tryBlock = Block( block.Expressions );

        var tryFinally = TryFinally( tryBlock, Block( updateExpressions ) );

        // Create the wrapping block.
        var blockExpressions = new List<Expression>();
        blockExpressions.AddRange( initExpressions );
        blockExpressions.Add( tryFinally );

        return Block(
            localVariables.Values,
            blockExpressions
        );
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly Dictionary<string, ParameterExpression> _locals;
        public ParameterReplacer( Dictionary<string, ParameterExpression> locals ) => _locals = locals;
        protected override Expression VisitParameter( ParameterExpression node ) =>
            node.Name != null && _locals.TryGetValue( node.Name, out var replacement )
                ? replacement
                : base.VisitParameter( node );
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
