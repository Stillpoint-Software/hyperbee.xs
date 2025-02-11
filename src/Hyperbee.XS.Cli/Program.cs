using Hyperbee.Xs.Cli.Commands;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli;

partial class Program
{
    public static int Main( string[] args )
    {
        var app = new CommandApp();

        app.Configure( config =>
        {

            config.AddCommand<ReplCommand>( "repl" );

#if NET9_0_OR_GREATER
            config.AddCommand<CompileCommand>( "compile" );
#endif
            config.AddBranch<RunSettings>( "run", run =>
            {
                run.AddCommand<RunFileCommand>( "file" );
                run.AddCommand<RunScriptCommand>( "script" );
            } );
        } );

        return app.Run( args );
    }
}
