using Hyperbee.Xs.Cli.Commands;
using Hyperbee.XS.Core;
using Spectre.Console;

namespace Hyperbee.Xs.Cli;

internal static class RunSettingsHelper
{
    public static IEnumerable<IParseExtension> GetExtensions( string value, TypeResolver typeResolver )
    {
        if ( string.IsNullOrWhiteSpace( value ) )
            yield break;

        // Split the string by semicolon IParseExtension in current loaded assemblies
        foreach ( var part in value.Split( ';' ) )
        {
            var extension = GetExtension( part, typeResolver );
            if ( extension != null )
                yield return extension;
        }

        static IParseExtension GetExtension( string value, TypeResolver typeResolver )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
                return default;

            try
            {
                var type = typeResolver.ResolveType( value );
                if ( type == null )
                {
                    AnsiConsole.MarkupInterpolated( $"[yellow]Warning: Could not find extension '{value}'[/]\n" );
                    return default;
                }

                if ( Activator.CreateInstance( type ) is IParseExtension extension )
                {
                    AnsiConsole.MarkupInterpolated( $"[green]Loaded:'{type.FullName}'[/]\n" );
                    return extension;
                }

            }
            catch ( Exception ex )
            {
                AnsiConsole.MarkupInterpolated( $"[yellow]Warning: Could not load extension '{value}': {ex.Message}[/]\n" );
            }
            return default;
        }
    }

    public static void LoadReferences( string value, ReferenceManager referenceManager )
    {
        if ( string.IsNullOrWhiteSpace( value ) )
            return;

#if NET9_0_OR_GREATER
        var span = value.AsSpan();
        foreach ( var segment in span.Split( ';' ) )
        {
            LoadAssembly( span[segment].ToString(), referenceManager );
        }
#else
        foreach ( var part in value.Split( ';' ) )
        {
            LoadAssembly( part, referenceManager );
        }
#endif

        return;

        static void LoadAssembly( string value, ReferenceManager referenceManager )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
                return;

            try
            {
                referenceManager.AddReference( referenceManager.LoadFromAssemblyPath( value ) );
            }
            catch ( Exception ex )
            {
                AnsiConsole.MarkupInterpolated( $"[yellow]Warning: Could not load assembly '{value}': {ex.Message}[/]\n" );
            }
        }
    }

    internal static async Task LoadPackages( string packages, ReferenceManager referenceManager )
    {
        if ( string.IsNullOrWhiteSpace( packages ) )
            return;

        foreach ( var package in packages.Split( ';' ) )
        {
            await referenceManager.LoadPackageAsync( package, version: null, source: null, new SpectreConsoleLogger() );
        }
    }
}
