using Hyperbee.Xs.Extensions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Hyperbee.Xs.Cli.Commands;

internal class RunSettings : CommandSettings
{
    [CommandOption( "-r|--references" )]
    public string References { get; init; }

    [CommandOption( "-p|--packages" )]
    public string Packages { get; init; }

    [CommandOption( "-e|--extensions" )]
    public string Extensions { get; init; }

    public XsConfig CreateConfig()
    {
        var referenceManager = new ReferenceManager();
        var typeResolver = new TypeResolver( referenceManager );

        IParseExtension[] extensions = null;

        AnsiConsole.Status()
            .Spinner( Spinner.Known.Default )
            .StartAsync( "Loading", async ctx =>
            {
                ctx.Status( "Loading References..." );
                RunSettingsHelper.LoadReferences( References, referenceManager );

                ctx.Status( "Loading Packages..." );
                await RunSettingsHelper.LoadPackages( Packages, referenceManager );

                ctx.Status( "Loading Extensions..." );
                extensions = RunSettingsHelper.GetExtensions( Extensions, typeResolver ).ToArray();

            } ).GetAwaiter().GetResult();

        return new XsConfig( typeResolver )
        {
            Extensions = [
                new StringFormatParseExtension(),
                new ForEachParseExtension(),
                new ForParseExtension(),
                new WhileParseExtension(),
                new UsingParseExtension(),
                new AsyncParseExtension(),
                new AwaitParseExtension(),
                //new DebugParseExtension(),
                new PackageParseExtension(),
                ..extensions ?? []
            ]
        };
    }

}
