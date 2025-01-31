#if NET9_0_OR_GREATER
using Hyperbee.Xs.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Hyperbee.Xs.Cli;

internal class CompileCommand : Command<CompileCommand.Settings>
{
    internal sealed class Settings : RunSettings
    {
        [Description( "File to compile" )]
        [CommandArgument( 0, "[file]" )]
        public string ScriptFile { get; init; }

        [Description( "File path for the saved assembly" )]
        [CommandOption( "-o|--output" )]
        public string Output { get; init; }

        [Description( "Assembly Name (can include Version, Culture and PublicKeyToken)" )]
        [CommandOption( "-a|--assemblyName" )]
        public string AssemblyName { get; init; }

        [Description( "Module Name" )]
        [CommandOption( "-m|--module" )]
        public string ModuleName { get; init; }

        [Description( "Class Name" )]
        [CommandOption( "-c|--class" )]
        public string ClassName { get; init; }

        [Description( "Function Name" )]
        [CommandOption( "-f|--function" )]
        public string FunctionName { get; init; }
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

            var result = Script.Compile( 
                script, 
                settings.AssemblyName, 
                settings.Output,
                settings.ModuleName,
                settings.ClassName,
                settings.FunctionName,
                references );

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
#endif
