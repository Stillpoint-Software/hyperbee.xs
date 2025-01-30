﻿using Hyperbee.Xs.Cli.Commands;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli;

partial class Program
{
    public static int Main( string[] args )
    {
        var app = new CommandApp();

        app.Configure( config =>
        {
            config.AddBranch<RunSettings>( "run", run => 
            {
                run.AddCommand<RunFileCommand>( "file" );
                run.AddCommand<RunScriptCommand>( "script" );

                run.AddCommand<ReplCommand>( "repl" );

#if NET9_0_OR_GREATER
                run.AddCommand<CompileCommand>( "compile" );
#endif
            } );
        } );

        return app.Run( args );
    }
}
