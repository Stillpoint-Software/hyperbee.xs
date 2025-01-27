﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Hyperbee.XS.System.Writer;

public class ExpressionWriter( ExpressionWriterContext context, Action<ExpressionWriter> dispose ) : IDisposable
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

        // Handle method parameters
        var parameters = methodInfo.GetParameters();
        var parameterTypes = parameters.Length > 0
            ? $"new[] {{ {string.Join( ", ", parameters.Select( p => $"typeof({GetTypeString( p.ParameterType )})" ) )} }}"
            : "Type.EmptyTypes";

        // Check if the method is generic
        if ( methodInfo.IsGenericMethodDefinition || methodInfo.IsGenericMethod )
        {
            // For generic method definitions, include a description to construct MakeGenericMethod
            var genericArguments = methodInfo.GetGenericArguments();
            var genericArgumentTypes = string.Join( ", ", genericArguments.Select( arg => $"typeof({GetTypeString( arg )})" ) );

            Write( $"typeof({declaringType}).GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.MakeGenericMethod({genericArgumentTypes})", indent: true );
        }
        else
        {
            // Non-generic method handling
            Write( $"typeof({declaringType}).GetMethod(\"{methodName}\", {parameterTypes})", indent: true );
        }
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

    public void WriteArgumentTypes( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Write( "," );

            Write( "\n" );
            Write( "new[] {\n", indent: true );

            Indent();

            var count = arguments.Count;

            for ( var i = 0; i < count; i++ )
            {
                Write( $"typeof({GetTypeString( arguments[i].Type )})", indent: true );

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

    public void WriteArguments( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Write( "," );

            Write( "\n" );

            var count = arguments.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( arguments[i] );

                if ( i < count - 1 )
                {
                    Write( "," );
                }

                Write( "\n" );
            }
        }
    }

    public void WriteParamsArguments( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Write( ",\n" );

            Write( "new[] {\n", indent: true );
            Indent();

            var count = arguments.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( arguments[i] );

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
            name = GenerateUniqueName( node.Name, node.Type, context.Parameters );

            context.Parameters.Add( node, name );

            Write( name, indent: true );

            context.ParameterOutput.Write( $"var {name} = {context.Prefix}Parameter( typeof({GetTypeString( node.Type )}), \"{name}\" );\n" );
        }
    }

    public void WriteParameters( ReadOnlyCollection<ParameterExpression> variables )
    {
        if ( variables.Count > 0 )
        {
            Write( "new[] {\n", indent: true );

            Indent();

            var count = variables.Count;

            for ( var i = 0; i < count; i++ )
            {
                WriteExpression( variables[i] );
                if ( i < count - 1 )
                {
                    Write( "," );
                }
                Write( "\n" );
            }

            Outdent();
            Write( "},\n", indent: true );
        }
    }

    public void WriteLabel( LabelTarget node )
    {
        if ( !context.Labels.ContainsKey( node ) )
        {
            var name = GenerateUniqueName( node.Name, node.Type, context.Labels );

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

    private static string GenerateUniqueName<T>( string name, Type type, Dictionary<T, string> lookup )
    {
        // Start with the parameter's name if it exists; otherwise, infer a name from the type
        var baseName = string.IsNullOrEmpty( name )
            ? InferName( type )
            : name;

        var uniqueName = baseName;
        var counter = 1;
        while ( lookup.ContainsValue( uniqueName ) || IsKeyword( uniqueName ) )
        {
            uniqueName = $"{baseName}{counter}";
            counter++;
        }

        return uniqueName;
    }

    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    ];

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static bool IsKeyword( string name ) => Keywords.Contains( name );

    private static string InferName( Type type )
    {
        var typeName = type.Name;

        if ( type.IsGenericType )
        {
            var backtickIndex = typeName.IndexOf( '`' );
            if ( backtickIndex > 0 )
            {
                typeName = typeName[..backtickIndex];
            }
        }

        var parts = SplitTypeNameByCasing( typeName );
        var shortParts = parts.Select( part => part.Length > 3 ? part[..3] : part );

        return string.Join( string.Empty, shortParts )
            .Insert( 0, shortParts.First()[..1].ToLowerInvariant() )
            .Remove( 1, 1 );

        static List<string> SplitTypeNameByCasing( ReadOnlySpan<char> typeName )
        {
            var parts = new List<string>();
            var start = 0;

            for ( var i = 1; i < typeName.Length; i++ )
            {
                if ( char.IsLower( typeName[i - 1] ) && char.IsUpper( typeName[i] ) )
                {
                    parts.Add( typeName[start..i].ToString() );
                    start = i;
                }
            }

            if ( start < typeName.Length )
            {
                parts.Add( typeName[start..].ToString() );
            }

            return parts;
        }
    }

    public void Dispose()
    {
        dispose?.Invoke( this );
    }

}
