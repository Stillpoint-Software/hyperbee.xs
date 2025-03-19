using System.Text.Json.Serialization;
using Hyperbee.XS.Core;
using Hyperbee.XS.Interactive;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Hyperbee.Xs.Interactive.Extensions;

public class ImportExtensionCommand : KernelCommand
{
    public string Extension { get; set; }

    public string Name { get; set; }

    [JsonPropertyName( "from" )]
    public string SourceKernelName { get; set; }

    internal static async Task HandleAsync( ImportExtensionCommand command, KernelInvocationContext context )
    {
        if ( context.HandlingKernel is not XsKernel xsKernel )
        {
            context.Fail( command, message: "Command only supported in XS kernel" );
            return;
        }

        if ( TryPackageExtension( command, context, xsKernel ) )
            return;

        await KernelExtension( command, context, xsKernel );
    }

    private static async Task KernelExtension( ImportExtensionCommand command, KernelInvocationContext context, XsKernel xsKernel )
    {
        var fromKernel = xsKernel.RootKernel.FindKernelByName( command.SourceKernelName );
        var fromName = command.Name;
        var supportedRequestValue = fromKernel.SupportsCommandType( typeof( RequestValue ) );

        if ( !supportedRequestValue )
        {
            context.Fail( command, message: $"Kernel {fromKernel} does not support command {nameof( RequestValue )}" );
            return;
        }

        var requestValue = new RequestValue( fromName );

        requestValue.SetParent( context.Command, true );

        var requestValueResult = await fromKernel.SendAsync( requestValue );

        switch ( requestValueResult.Events[^1] )
        {
            case CommandSucceeded:
                var valueProduced = requestValueResult.Events.OfType<ValueProduced>().SingleOrDefault();

                if ( valueProduced is not null )
                {
                    if ( xsKernel.SupportsCommandType( typeof( SendValue ) ) )
                    {
                        if ( valueProduced.Value is not IParseExtension value )
                        {
                            context.Fail( command, message: $"Value {valueProduced.Name} is not an IParseExtension" );
                            return;
                        }

                        xsKernel.Parser.Value.AddExtensions( [value] );
                    }
                    else
                    {
                        throw new CommandNotSupportedException( typeof( SendValue ), xsKernel );
                    }
                }

                break;

            case CommandFailed:
                break;
        }
    }

    private static bool TryPackageExtension( ImportExtensionCommand command, KernelInvocationContext context, XsKernel xsKernel )
    {
        var parser = xsKernel.Parser.Value;
        var typeResolver = xsKernel.TypeResolver;

        if ( !string.IsNullOrEmpty( command.Extension ) )
        {
            parser.AddExtensions( [.. GetExtensions( command.Extension, typeResolver, command, context )] );
            return true;
        }

        return false;
    }

    private static IEnumerable<dynamic> GetExtensions( string value, TypeResolver typeResolver, KernelCommand command, KernelInvocationContext context )
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

public static class KernelExtensions
{
    public static void UseExtensions( this XsBaseKernel kernel )
    {
        KernelCommandEnvelope.RegisterCommand<ImportExtensionCommand>();

        kernel.AddDirective<ImportExtensionCommand>( new KernelActionDirective( "#!import" )
        {
            Description = "Adds a parser extension to XS that enhances the languages syntax",
            Parameters =
                [
                    new("--extension")
                    {
                        AllowImplicitName = true,
                        Description = $"Name of an {nameof(IParseExtension)} class from an existing package/NuGet"
                    },
                    new("--from")
                    {
                        Description = "Name of the kernel to import the extension from"
                    },
                    new("--name")
                    {
                        Description = $"Name of the variable from another kernel that implements {nameof(IParseExtension)}"
                    }
                ],
            KernelCommandType = typeof( ImportExtensionCommand ),
        }, ImportExtensionCommand.HandleAsync );

    }

}
