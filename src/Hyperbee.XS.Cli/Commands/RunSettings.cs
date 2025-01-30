using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class RunSettings : CommandSettings
{
    [CommandOption( "-r|--references" )]
    public string References { get; init; }
}
