using System.Reflection;
using Spectre.Console;

namespace Hyperbee.Xs.Cli.Converters;

public static class AssemblyHelper
{
    public static List<Assembly> GetAssembly( string value )
    {
        var assemblies = new List<Assembly>();

        if ( string.IsNullOrWhiteSpace( value ) )
            return assemblies;

#if NET9_0_OR_GREATER
        var span = value.AsSpan();
        foreach ( var segment in span.Split( ';' ) )
        {
            assemblies.Add( GetAssembly( span[segment].ToString() ) );
        }
#else
        foreach ( var part in value.Split( ';' ) )
        {
            assemblies.Add( GetAssembly( part ) );
        }
#endif

        return assemblies;

        static Assembly GetAssembly( string value )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
                return default;

            try
            {
                var assembly = File.Exists( value )
                    ? Assembly.LoadFrom( value )
                    : Assembly.Load( value );

                return assembly;
            }
            catch ( Exception ex )
            {
                AnsiConsole.MarkupInterpolated( $"[yellow]Warning: Could not load assembly '{value}': {ex.Message}[/]\n" );
            }

            return default;
        }
    }
}
