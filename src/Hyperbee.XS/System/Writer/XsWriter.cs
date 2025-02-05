using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.System.Writer;

public class XsWriter( XsWriterContext context, Action<XsWriter> dispose ) : IDisposable
{
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public Expression WriteExpression( Expression node )
    {
        return context.Visitor.Visit( node );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void WriteMemberInfo( MemberInfo memberInfo )
    {
        Write( memberInfo.Name );
    }

    public void WriteMethodInfo( MethodInfo methodInfo )
    {
        Write( methodInfo.Name );
    }

    public void WriteConstructorInfo( ConstructorInfo constructorInfo )
    {
        var declaringType = GetTypeString( constructorInfo.DeclaringType );
        Write( declaringType );
    }

    public void WriteTerminated()
    {
        if ( !context.SkipTerminated )
            Write( ";\n" );

        context.SkipTerminated = false;
    }

    public string GetTypeString( Type type )
    {
        if ( CSharpTypes.TryGetValue( type, out var typeName ) )
        {
            return typeName;
        }

        if ( type.IsGenericType )
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            // Get the base type name without the backtick
            var baseTypeName = genericTypeDefinition.Name;
            var backtickIndex = baseTypeName.IndexOf( '`' );
            if ( backtickIndex > 0 )
            {
                baseTypeName = baseTypeName[..backtickIndex];
            }

            // Recursively build the string for generic arguments without wrapping in typeof
            var genericArgumentsString = string.Join( ", ", genericArguments.Select( GetTypeString ) );
            return $"{baseTypeName}<{genericArgumentsString}>";
        }

        return type.Name;
    }

    public void WriteType( Type type )
    {
        Write( GetTypeString( type ) );
    }

    public void WriteExpressions<T>( ReadOnlyCollection<T> collection, bool firstArgument = false ) where T : Expression
    {

        if ( collection.Count > 0 )
        {
            var count = collection.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( collection[i] );
            }
        }
    }

    public void WriteParameter( ParameterExpression node )
    {

        if ( context.Parameters.TryGetValue( node, out var name ) )
        {
            Write( $"{name}" );
        }
        else
        {
            name = NameGenerator.GenerateUniqueName( node.Name, node.Type, context.Parameters );

            context.Parameters.Add( node, name );

            Write( $"var {node.Name}", indent: true );
        }
    }

    public void Write( object value, bool indent = false )
    {
        if ( indent )
        {
            for ( var i = 0; i < context.IndentDepth; i++ )
            {
                context.ExpressionOutput.Write( context.Indention );
            }
        }

        context.ExpressionOutput.Write( value );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Indent()
    {
        context.IndentDepth++;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Outdent()
    {
        context.IndentDepth--;
    }

    private static readonly Dictionary<Type, string> CSharpTypes = new()
    {
        { typeof(int), "int" },
        { typeof(short), "short" },
        { typeof(string), "string" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(long), "long" },
        { typeof(sbyte), "sbyte" },
        { typeof(ushort), "ushort" },
        { typeof(uint), "uint" },
        { typeof(ulong), "ulong" },
        { typeof(object), "object" },
        { typeof(void), "null" }
    };

    public void Dispose()
    {
        dispose?.Invoke( this );
    }

}
