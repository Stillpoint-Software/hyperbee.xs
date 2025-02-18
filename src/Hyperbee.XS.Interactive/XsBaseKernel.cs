using Hyperbee.Xs.Extensions;
using Hyperbee.Xs.Interactive.Extensions;
using Hyperbee.XS.Core;
using Microsoft.DotNet.Interactive;

namespace Hyperbee.XS.Interactive;

public class XsBaseKernel : Kernel
{
    protected XsConfig Config;
    protected ParseScope Scope = new();
    protected Dictionary<string, object> State = [];
    protected Lazy<XsParser> Parser => new( () => new XsParser( Config ) );

    public XsBaseKernel( string name ) : base( name )
    {
        var typeResolver = TypeResolver.Create(
            typeof( object ).Assembly,
            typeof( Enumerable ).Assembly,
            typeof( DisplayExtensions ).Assembly // pull in .NET Interactive helpers?
        );

        Config = new XsConfig( typeResolver )
        {
            Extensions = [
                new StringFormatParseExtension(),
                new ForEachParseExtension(),
                new ForParseExtension(),
                new WhileParseExtension(),
                new UsingParseExtension(),
                new AsyncParseExtension(),
                new AwaitParseExtension(),
                new PackageParseExtension(),

                // Notebook Helpers
                new DisplayParseExtension()
            ]
        };

        Scope.EnterScope( FrameType.Method );

        RegisterForDisposal( () =>
        {
            Scope.ExitScope();
            Scope = null;
            State = null;
            Config = null;
        } );
    }

}
