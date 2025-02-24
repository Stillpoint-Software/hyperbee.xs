using Hyperbee.Xs.Extensions;
using Hyperbee.Xs.Interactive.Extensions;
using Hyperbee.XS.Core;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

#if NET9_0_OR_GREATER
using Microsoft.DotNet.Interactive.PackageManagement;
using NuGet.Protocol.Plugins;
#endif

namespace Hyperbee.XS.Interactive;

public class XsBaseKernel : Kernel
{
    protected TypeResolver TypeResolver { get; }

    protected XsConfig Config;
    protected ParseScope Scope = new();
    protected Dictionary<string, object> State = [];
    protected Lazy<XsParser> Parser => new( () => new XsParser( Config ) );

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

        KernelCommandEnvelope.RegisterCommand<ExtensionCommand>();
        AddDirective<ExtensionCommand>( new KernelActionDirective( "#!extensions" )
        {
            Description = "",
            Parameters =
                [
                    new("--extension")
                    {
                        AllowImplicitName = true,
                        Required = true
                    }
                ],
            KernelCommandType = typeof( ExtensionCommand ),
        },
        ( command, ctx ) =>
        {
            Parser.Value.AddExtensions( [.. GetExtensions( command.Extension, TypeResolver, command, ctx )] );
            return Task.CompletedTask;
        } );

        Scope.EnterScope( FrameType.Method );

        RegisterForDisposal( () =>
        {
            Scope.ExitScope();
            Scope = null;
            State = null;
            Config = null;
        } );
    }

    public static IEnumerable<dynamic> GetExtensions( string value, TypeResolver typeResolver, KernelCommand command, KernelInvocationContext context )
    {
        if ( string.IsNullOrWhiteSpace( value ) )
            yield break;

        // Split the string by semicolon IParseExtension in current loaded assemblies
        foreach ( var part in value.Split( ';' ) )
        {
            var extension = GetExtension( part, typeResolver, command, context );
            if ( extension != null )
            {
                yield return extension;
            } 
        }

        static IParseExtension GetExtension( string value, TypeResolver typeResolver, KernelCommand command, KernelInvocationContext context )
        {
            if ( string.IsNullOrWhiteSpace( value ) ) 
            {
                context.Fail( command, message: "Missing extension name" );
                return default;
            }

            try
            {
                var type = typeResolver.ResolveType( value );
                if ( type == null )
                {
                    context.Fail( command, message: $"Could not resolve type for extension {value}" );
                    return default;
                }

                var instance = Activator.CreateInstance( type );
                if ( instance == null )
                {
                    context.Fail( command, message: $"Could not create instance of extension {value}" );
                    return default;
                }

                if ( instance is not IParseExtension extension )
                {
                    context.Fail( command, message: $"Extension {value} does not implement {nameof( IParseExtension )}" );
                    return default;
                }

                $"Loaded extension {value}".Display();
                return extension;
            }
            catch ( Exception ex )
            {
                context.Fail( command, message: ex.Message );
            }
            return default;
        }
    }
}
