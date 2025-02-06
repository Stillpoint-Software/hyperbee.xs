using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.Core.Writer;

public sealed class ExpressionWriter( ExpressionWriterContext context, Action<ExpressionWriter> dispose ) : IDisposable
{
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public Expression WriteExpression( Expression node )
    {
        return context.Visitor.Visit( node );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void WriteMemberInfo( MemberInfo memberInfo )
    {
        // TODO: Improve lookup
        Write( $"typeof({GetTypeString( memberInfo.DeclaringType )}).GetMember(\"{memberInfo.Name}\")[0]", indent: true );
    }

    public void WriteMethodInfo( MethodInfo methodInfo )
    {
        var methodName = methodInfo.Name;
        var declaringType = GetTypeString( methodInfo.DeclaringType );
        var parameters = methodInfo.GetParameters();

        var parameterTypes = parameters.Length > 0
            ? $"new[] {{ {string.Join( ", ", parameters.Select( p => $"typeof({GetTypeString( p.ParameterType )})" ) )} }}"
            : "Type.EmptyTypes";

        if ( methodInfo.IsGenericMethodDefinition || methodInfo.IsGenericMethod )
        {
            // For generic method definitions, include a description to construct MakeGenericMethod
            var genericArguments = methodInfo.GetGenericArguments();
            var genericArgumentTypes = string.Join( ", ", genericArguments.Select( arg => $"typeof({GetTypeString( arg )})" ) );

            Write( $"typeof({declaringType}).GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.MakeGenericMethod({genericArgumentTypes})", indent: true );
        }
        else
        {
            Write( $"typeof({declaringType}).GetMethod(\"{methodName}\", {parameterTypes})", indent: true );
        }
    }

    public void WriteConstructorInfo( ConstructorInfo constructorInfo )
    {
        var declaringType = GetTypeString( constructorInfo.DeclaringType );
        var parameters = constructorInfo.GetParameters();

        var parameterTypes = parameters.Length > 0
            ? $"new[] {{ {string.Join( ", ", parameters.Select( p => $"typeof({GetTypeString( p.ParameterType )})" ) )} }}"
            : "Type.EmptyTypes";

        Write( $"typeof({declaringType}).GetConstructor({parameterTypes})", indent: true );
    }

    public string GetTypeString( Type type )
    {
        if ( !context.Usings.Contains( type.Namespace ) )
        {
            context.Usings.Add( type.Namespace );
        }

        if ( type == typeof( void ) )
        {
            return "void";
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
        Write( $"typeof({GetTypeString( type )})", indent: true );
    }

    public void WriteExpressions<T>( ReadOnlyCollection<T> collection, bool firstArgument = false ) where T : Expression
    {
        if ( collection.Count > 0 )
        {
            if ( !firstArgument )
                Write( "," );

            Write( "\n" );

            var count = collection.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( collection[i] );

                if ( i < count - 1 )
                {
                    Write( "," );
                }

                Write( "\n" );
            }
        }
    }

    public void WriteParamExpressions<T>( ReadOnlyCollection<T> collection, bool firstArgument = false ) where T : Expression
    {
        if ( collection.Count > 0 )
        {
            if ( !firstArgument )
                Write( ",\n" );

            Write( "new[] {\n", indent: true );
            Indent();

            var count = collection.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( collection[i] );

                if ( i < count - 1 )
                {
                    Write( "," );
                }

                Write( "\n" );
            }

            Outdent();
            Write( "}", indent: true );
        }
    }

    public void WriteParameter( ParameterExpression node )
    {
        if ( context.Parameters.TryGetValue( node, out var name ) )
        {
            Write( name, indent: true );
        }
        else
        {
            name = NameGenerator.GenerateUniqueName( node.Name, node.Type, context.Parameters );

            context.Parameters.Add( node, name );

            Write( name, indent: true );

            context.ParameterOutput.Write( $"var {name} = {context.Prefix}Parameter( typeof({GetTypeString( node.Type )}), \"{name}\" );\n" );
        }
    }

    public void WriteLabel( LabelTarget node )
    {
        if ( !context.Labels.ContainsKey( node ) )
        {
            var name = NameGenerator.GenerateUniqueName( node.Name, node.Type, context.Labels );

            context.Labels.Add( node, name );

            if ( node.Type != null && node.Type != typeof( void ) )
            {
                context.LabelOutput.Write( $"var {name} = {context.Prefix}Label( typeof({GetTypeString( node.Type )}), \"{name}\" );\n" );
            }
            else
            {
                context.LabelOutput.Write( $"var {name} = {context.Prefix}Label( \"{name}\" );\n" );
            }
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

    public void Dispose() => dispose?.Invoke( this );

}
