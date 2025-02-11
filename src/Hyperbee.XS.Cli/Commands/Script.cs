
using System.Linq.Expressions;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Reflection.Emit;  // For PersistedAssemblyBuilder
using System.Runtime.InteropServices;
using System.Runtime.Loader;

#endif
using Hyperbee.XS;
using Hyperbee.XS.Core.Writer;
using Spectre.Console;

namespace Hyperbee.Xs.Cli.Commands;

internal static class Script
{
#if NET9_0_OR_GREATER

    internal static string Compile( 
        string script, 
        string outputAssemblyName, 
        string outputFile, 
        string outputModuleName = null,
        string outputClassName = null,
        string outputMethodName = null,
        XsConfig config = null )
    {
        outputModuleName ??= "DynamicModule";
        outputClassName ??= "DynamicClass";
        outputMethodName ??= "DynamicMethod";

        var parser = new XsParser( config );

        AnsiConsole.Status()
            .Spinner( Spinner.Known.Default )
            .Start( "Compiling", ctx =>
            {
                ctx.Status( "Parsing..." );

                var expression = parser.Parse( script );

                ctx.Status( "Building Types..." );

                var currentFramework = FrameworkLocator.GetCurrentFramework();
                var frameworkPath = FrameworkLocator.GetFrameworkPath( currentFramework );
                var resolver = new PathAssemblyResolver( Directory.GetFiles( frameworkPath, "*.dll" ) );

                AnsiConsole.MarkupInterpolated( $"[purple]Using:[/] {currentFramework} ([grey]{frameworkPath}[/])\n" );

                using MetadataLoadContext context = new MetadataLoadContext( resolver );
                var coreAssembly = context.CoreAssembly;

                // Create the PersistedAssemblyBuilder
                // See: https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-reflection-emit-persistedassemblybuilder
                var assemblyBuilder = new PersistedAssemblyBuilder(
                    new AssemblyName( outputAssemblyName ),
                    coreAssembly
                );

                // Define a dynamic module
                var moduleBuilder = assemblyBuilder.DefineDynamicModule( outputModuleName );

                // Define a public static class in the assembly
                var typeBuilder = moduleBuilder.DefineType(
                    outputClassName,
                    TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed,
                    coreAssembly.GetType( typeof( object ).FullName )
                );

                // Define a public method (that has no parameters)
                var methodBuilder = typeBuilder.DefineMethod(
                    outputMethodName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    coreAssembly.GetType( expression.Type.FullName ), // expression.Type,
                    Type.EmptyTypes
                );

                // Get an ILGenerator and emit a body for the expression
                var il = methodBuilder.GetILGenerator();
                var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
                var lambda = Expression.Lambda( delegateType, expression );

                ctx.Status( "Compiling to IL..." );

                FastExpressionCompiler.ExpressionCompiler.CompileFastToIL( lambda, il );

                // Create the type
                typeBuilder.CreateType();

                ctx.Status( "Saving..." );

                // Save the assembly to a DLL file
                assemblyBuilder.Save( outputFile );
            } );

        return "Success";
    }

    public static class FrameworkLocator
    {
        private static readonly string DotNetRoot = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ), "dotnet" );

        public static string GetFrameworkPath( string version )
        {
            var frameworkPath = Path.Combine( DotNetRoot, "shared", "Microsoft.NETCore.App", version );
            return Directory.Exists( frameworkPath ) ? frameworkPath : null;
        }

        public static string GetCurrentFramework()
        {
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            var version = frameworkDescription.Split( ' ' ).LastOrDefault();
            return version ?? "Unknown";
        }
    }

#endif

    internal static string Execute( string script, XsConfig config = null )
    {
        var parser = new XsParser( config );

        var expression = parser.Parse( script );

        var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
        var lambda = Expression.Lambda( delegateType, expression );
        var compiled = lambda.Compile();
        var result = compiled.DynamicInvoke();

        return result?.ToString() ?? "null";
    }

    internal static string Show( string script, XsConfig config = null )
    {
        var parser = new XsParser( config );

        var expression = parser.Parse( script );

        return expression?.ToExpressionString() ?? "null";
    }
}

