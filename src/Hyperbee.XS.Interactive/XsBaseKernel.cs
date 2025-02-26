using System.Text.Json.Serialization;
using Hyperbee.Collections.Extensions;
using Hyperbee.Xs.Extensions;
using Hyperbee.Xs.Interactive.Extensions;
using Hyperbee.XS.Core;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;


#if NET9_0_OR_GREATER
using Microsoft.DotNet.Interactive.PackageManagement;
#endif

namespace Hyperbee.XS.Interactive;

public class XsBaseKernel : Kernel
{
    internal TypeResolver TypeResolver { get; }

    protected XsConfig Config;
    protected ParseScope Scope = new();
    protected Dictionary<string, object> State = [];
    internal Lazy<XsParser> Parser => new( () => new XsParser( Config ) );

    public XsBaseKernel( string name ) : base( name )
    {
        KernelInfo.LanguageVersion = "1.2";
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - Expression Script";

        TypeResolver = TypeResolver.Create(
            typeof( object ).Assembly,
            typeof( Enumerable ).Assembly,
            typeof( IParseExtension ).Assembly
        );

        Config = new XsConfig( TypeResolver )
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
                new PackageSourceParseExtension(),

                // Notebook Helpers
                new DisplayParseExtension()
            ]
        };

#if NET9_0_OR_GREATER
        this.UseNugetDirective( async ( kernel, references ) =>
        {
            foreach ( var reference in references )
            {
                await PackageParseExtension.Resolve( reference.PackageName, reference.PackageVersion, TypeResolver );
            }
        } );
#endif

        this.UseExtensions();

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
