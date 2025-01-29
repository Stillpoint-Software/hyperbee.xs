using System.Reflection;
using CliFx;
using Hyperbee.Xs.Tools.Commands;

namespace Hyperbee.XS.Tools;

class Program
{
    static async Task<int> Main( string[] args ) => await new CliApplicationBuilder()
        .AddCommand<RunCommand>()
        .AddCommand<CompileCommand>()
        .AddCommand<ReplCommand>()
        .Build()
        .RunAsync( args );
}
