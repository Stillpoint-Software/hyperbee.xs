
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Hyperbee.XS;
using Hyperbee.XS.System.Writer;

namespace Hyperbee.Xs.Cli.Commands;

internal static class Script
{
#if NET9_0_OR_GREATER

    internal static string Compile( string script, string outputAssemblyName, string file, IReadOnlyCollection<Assembly> references = null )
    {
        //var outputAssemblyName = "MyDynamicAssembly";
        var outputModuleName = "MyDynamicModule";
        var outputClassName = "MyDynamicClass";
        var outputMethodName = "HelloWorld";

        var parser = new XsParser(
            new XsConfig
            {
                References = references
            } 
        );

        var expression = parser.Parse( script );

        // Define the assembly name
        var assemblyName = new AssemblyName( outputAssemblyName );

        // Create the PersistedAssemblyBuilder
        var assemblyBuilder = new PersistedAssemblyBuilder(
            assemblyName,
            typeof( object ).Assembly
        );

        // Define a dynamic module
        var moduleBuilder = assemblyBuilder.DefineDynamicModule( outputModuleName );

        // Define a public class named 'MyDynamicClass' in the assembly
        var typeBuilder = moduleBuilder.DefineType(
            outputClassName,
            TypeAttributes.Public
        );

        // Define a public method named 'HelloWorld' that returns void and has no parameters
        var methodBuilder = typeBuilder.DefineMethod(
            outputMethodName,
            MethodAttributes.Public | MethodAttributes.Static,
            expression.Type,
            Type.EmptyTypes
        );

        // Get an ILGenerator and emit a body for the 'HelloWorld' method
        var ilGenerator = methodBuilder.GetILGenerator();
        var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
        var lambda = Expression.Lambda( delegateType, expression );

        FastExpressionCompiler.ExpressionCompiler.CompileFastToIL( lambda, ilGenerator );

        // Create the type
        typeBuilder.CreateType();

        // Save the assembly to a DLL file
        assemblyBuilder.Save( file );

        return "Successfully saved";
    }

#endif

    internal static string Execute( string script, IReadOnlyCollection<Assembly> references = null )
    {
        var parser = new XsParser(
            new XsConfig
            {
                References = references
            }
        );

        var expression = parser.Parse( script );

        var delegateType = typeof( Func<> ).MakeGenericType( expression.Type );
        var lambda = Expression.Lambda( delegateType, expression );
        var compiled = lambda.Compile();
        var result = compiled.DynamicInvoke();

        return result?.ToString() ?? "null";
    }

    internal static string Show( string script, IReadOnlyCollection<Assembly> references = null )
    {
        var parser = new XsParser(
            new XsConfig
            {
                References = references
            }
        );

        var expression = parser.Parse( script );

        return expression?.ToExpressionString() ?? "null";
    }
}

