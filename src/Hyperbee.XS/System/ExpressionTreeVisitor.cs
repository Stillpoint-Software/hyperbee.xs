﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hyperbee.XS.System;

public static class ExpressionTreeExtensions
{
    public static string ToExpressionTreeString( this Expression expression, ExpressionTreeVisitorConfig config = null )
    {
        return new ExpressionTreeVisitor().Convert( expression, config ?? new ExpressionTreeVisitorConfig() );
    }
}

public record ExpressionTreeVisitorConfig(
    string Prefix = "Expression.",
    char Indentation = '\t',
    string Variable = "expression" );


public class ExpressionTreeVisitor : ExpressionVisitor
{
    private readonly HashSet<string> _usings = [
        "System",
        "System.Linq.Expressions",
    ];
    private readonly Dictionary<ParameterExpression, string> _parameters = [];
    private readonly Dictionary<LabelTarget, string> _labels = [];

    private readonly StringBuilder _parameterOutput = new();
    private readonly StringBuilder _labelOutput = new();
    private readonly StringBuilder _expressionOutput = new();

    private int _depth = 0;
    private ExpressionTreeVisitorConfig _config;

    internal string Convert( Expression expression, ExpressionTreeVisitorConfig config )
    {
        _config = config;

        Visit( expression );

        var usings = string.Join( '\n', _usings.Select( u => $"using {u};" ) );

        return $"{usings}\n\n{_parameterOutput}\n{_labelOutput}\nvar {_config.Variable} = {_expressionOutput};";
    }

    protected override Expression VisitDebugInfo( DebugInfoExpression node )
    {
        return base.VisitDebugInfo( node );
    }

    protected override Expression VisitBinary( BinaryExpression node )
    {
        EnterExpression( $"{node.NodeType}" );

        Visit( node.Left );
        Append( ",\n" );

        Visit( node.Right );

        if ( node.Method != null )
        {
            Append( ",\n" );
            AppendIndented( $"{GetMethodInfoString( node.Method )}," );
        }

        if ( node.Conversion != null )
        {
            Append( ",\n" );
            Visit( node.Conversion );
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitBlock( BlockExpression node )
    {
        EnterExpression( "Block" );

        if ( node.Variables.Count > 0 )
        {
            AppendIndented( "new[] {\n" );

            _depth++;
            for ( int i = 0; i < node.Variables.Count; i++ )
            {
                Visit( node.Variables[i] );
                if ( i < node.Variables.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "},\n" );
        }

        for ( int i = 0; i < node.Expressions.Count; i++ )
        {
            Visit( node.Expressions[i] );
            if ( i < node.Expressions.Count - 1 )
            {
                Append( ",\n" );
            }
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitConditional( ConditionalExpression node )
    {
        EnterExpression( "Condition" );

        Visit( node.Test );
        Append( ",\n" );
        Visit( node.IfTrue );
        Append( ",\n" );
        Visit( node.IfFalse );
        Append( ",\n" );
        AppendIndented( $"typeof({GetTypeString( node.Type )})" );

        ExitExpression();
        return node;
    }

    protected override Expression VisitConstant( ConstantExpression node )
    {
        var value = node.Value;
        EnterExpression( "Constant", newLine: false );

        switch ( value )
        {
            case string:
                Append( $"\"{value}\"" );
                break;

            case bool boolValue:
                Append( boolValue ? "true" : "false" );
                break;

            case null:
                Append( $"null, typeof({GetTypeString( node.Type )})" );
                break;

            default:
                Append( value );
                break;
        }

        ExitExpression( newLine: false );
        return node;
    }

    protected override Expression VisitDefault( DefaultExpression node )
    {
        if ( node.Type != null && node.Type != typeof( void ) )
        {
            EnterExpression( "Default" );
            AppendIndented( $"typeof({GetTypeString( node.Type )})" );
            ExitExpression();
        }
        else
        {
            EnterExpression( "Empty", newLine: false );
            ExitExpression( newLine: false );
        }

        return node;
    }

    protected override ElementInit VisitElementInit( ElementInit node )
    {
        EnterExpression( "ElementInit" );

        AppendIndented( GetMethodInfoString( node.AddMethod ) );
        AppendArguments( node.Arguments );

        ExitExpression();
        return node;
    }

    protected override Expression VisitExtension( Expression node )
    {
        Visit( node.Reduce() );
        return node;
    }

    protected override Expression VisitDynamic( DynamicExpression node )
    {
        return base.VisitDynamic( node );
    }

    protected override Expression VisitGoto( GotoExpression node )
    {
        EnterExpression( $"{node.Kind}" );

        if ( _labels.TryGetValue( node.Target, out var lableTarget ) )
        {
            AppendIndented( lableTarget );
        }
        else
        {
            VisitLabelTarget( node.Target );
            _labels.TryGetValue( node.Target, out lableTarget );
            AppendIndented( lableTarget );
        }

        if ( node.Value != null )
        {
            Append( ",\n" );
            Visit( node.Value );
        }

        if ( node.Type != null && node.NodeType == ExpressionType.Default && node.Type != typeof( void ) )
        {
            Append( ",\n" );
            AppendIndented( $"typeof({GetTypeString( node.Type )})" );
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitIndex( IndexExpression node )
    {

        if ( node.Indexer != null )
        {
            EnterExpression( "MakeIndex" );

            Visit( node.Object );
            Append( ",\n" );

            AppendIndented( $"typeof({GetTypeString( node.Indexer.DeclaringType )}).GetProperty(\"{node.Indexer.Name}\", \n" );

            _depth++;
            AppendIndented( "new[] {\n" );

            _depth++;
            var parameters = node.Indexer.GetIndexParameters();

            for ( var i = 0; i < parameters.Length; i++ )
            {
                AppendIndented( $"typeof({GetTypeString( parameters[i].ParameterType )})" );
                if ( i < parameters.Length - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }

            _depth--;
            AppendIndented( "}\n" );

            _depth--;
            AppendIndented( $")" );

            AppendParamsArguments( node.Arguments );

            ExitExpression();
            return node;
        }
        else
        {
            EnterExpression( "ArrayAccess" );

            Visit( node.Object );

            AppendArguments( node.Arguments );

            ExitExpression();
            return node;

        }

    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        EnterExpression( "Invoke" );

        Visit( node.Expression );

        AppendArguments( node.Arguments );

        ExitExpression();
        return node;
    }

    protected override Expression VisitLambda<T>( Expression<T> node )
    {
        EnterExpression( "Lambda" );

        Visit( node.Body );

        if ( node.Parameters.Count > 0 )
        {
            Append( ",\n" );
            AppendIndented( "new[] {\n" );

            _depth++;
            for ( int i = 0; i < node.Parameters.Count; i++ )
            {
                Visit( node.Parameters[i] );
                if ( i < node.Parameters.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "}" );
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitListInit( ListInitExpression node )
    {
        EnterExpression( "ListInit" );

        Visit( node.NewExpression );
        Append( ",\n" );

        AppendInitializers( node.Initializers );

        ExitExpression();
        return node;
    }

    protected override Expression VisitMember( MemberExpression node )
    {
        EnterExpression( "MakeMemberAccess" );

        Visit( node.Expression );
        Append( $",\n" );

        AppendIndented( GetMemberInfoString( node.Member ) );

        ExitExpression();
        return node;
    }

    protected override Expression VisitMemberInit( MemberInitExpression node )
    {
        return base.VisitMemberInit( node );
    }

    protected override Expression VisitMethodCall( MethodCallExpression node )
    {
        EnterExpression( "Call" );

        if ( node.Object != null )
        {
            Visit( node.Object );
            Append( ",\n" );
        }
        else
        {
            AppendIndented( $"null,\n" );
        }

        AppendIndented( GetMethodInfoString( node.Method ) );

        AppendArguments( node.Arguments );

        ExitExpression();
        return node;
    }

    protected override Expression VisitNew( NewExpression node )
    {
        EnterExpression( "New" );

        AppendIndented( $"typeof({GetTypeString( node.Type )}).GetConstructor(" );

        if ( node.Arguments == null || node.Arguments.Count == 0 )
        {
            Append( "\n" );
            AppendIndented( "Type.EmptyTypes" );
        }
        else
        {
            AppendArgumentTypes( node.Arguments, firstArgument: true );
        }
        Append( ")" );

        AppendArguments( node.Arguments );

        ExitExpression();
        return node;
    }

    protected override Expression VisitNewArray( NewArrayExpression node )
    {
        EnterExpression( $"{node.NodeType}" );

        if ( node.NodeType == ExpressionType.NewArrayBounds )
        {
            AppendIndented( $"typeof({GetTypeString( node.Type )})" );
        }
        else
        {
            AppendIndented( $"typeof({GetTypeString( node.Type.GetElementType() )})" );
        }

        AppendArguments( node.Expressions );
        ExitExpression();
        return node;
    }

    protected override Expression VisitParameter( ParameterExpression node )
    {
        if ( _parameters.TryGetValue( node, out var name ) )
        {
            AppendIndented( name );
        }
        else
        {
            name = GenerateUniqueName( node.Name, node.Type );
            _parameters.Add( node, name );
            AppendIndented( name );
            _parameterOutput.Append( $"var {name} = {_config.Prefix}Parameter( typeof({GetTypeString( node.Type )}), \"{name}\" );\n" );
        }

        return node;
    }

    protected override Expression VisitRuntimeVariables( RuntimeVariablesExpression node )
    {
        return base.VisitRuntimeVariables( node );
    }

    protected override Expression VisitLabel( LabelExpression node )
    {
        EnterExpression( "Label" );

        if ( _labels.TryGetValue( node.Target, out var lableTarget ) )
        {
            AppendIndented( lableTarget );
        }
        else
        {
            VisitLabelTarget( node.Target );
            _labels.TryGetValue( node.Target, out lableTarget );
            AppendIndented( lableTarget );
        }

        if ( node.DefaultValue != null )
        {
            Append( ",\n" );
            Visit( node.DefaultValue );
        }

        ExitExpression();
        return node;
    }

    protected override LabelTarget VisitLabelTarget( LabelTarget node )
    {
        if ( !_labels.ContainsKey( node ) )
        {
            var name = GenerateLabelUniqueName( node.Name, node.Type );
            _labels.Add( node, name );
            if ( node.Type != null && node.Type != typeof( void ) )
            {
                _labelOutput.Append( $"var {name} = {_config.Prefix}Label( typeof({GetTypeString( node.Type )}), \"{name}\" );\n" );
            }
            else
            {
                _labelOutput.Append( $"var {name} = {_config.Prefix}Label( \"{name}\" );\n" );
            }
        }

        return node;
    }

    protected override Expression VisitLoop( LoopExpression node )
    {
        EnterExpression( "Loop" );

        Visit( node.Body );

        if ( node.BreakLabel != null )
        {
            VisitLabelTarget( node.BreakLabel );
            if ( _labels.TryGetValue( node.BreakLabel, out var breakLabel ) )
            {
                Append( ",\n" );
                AppendIndented( breakLabel );
            }
        }

        if ( node.ContinueLabel != null )
        {
            VisitLabelTarget( node.ContinueLabel );
            if ( _labels.TryGetValue( node.ContinueLabel, out var continueLabel ) )
            {
                Append( ",\n" );
                AppendIndented( continueLabel );
            }
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitSwitch( SwitchExpression node )
    {
        EnterExpression( "Switch" );

        Visit( node.SwitchValue );
        if ( node.DefaultBody != null )
        {
            Append( ",\n" );
            Visit( node.DefaultBody );
        }

        if ( node.Cases != null && node.Cases.Count > 0 )
        {
            AppendSwitchCases( node.Cases );
        }

        ExitExpression();
        return node;
    }

    protected override SwitchCase VisitSwitchCase( SwitchCase node )
    {
        EnterExpression( "SwitchCase" );

        Visit( node.Body );
        AppendArguments( node.TestValues );

        ExitExpression();
        return node;
    }

    protected override Expression VisitTry( TryExpression node )
    {
        EnterExpression( "TryCatchFinally" );

        Visit( node.Body );
        Append( ",\n" );

        if ( node.Finally != null )
        {
            Visit( node.Finally );
        }
        else
        {
            AppendIndented( "null" );
        }

        AppendCatchBlocks( node.Handlers );

        ExitExpression();
        return node;
    }

    protected override CatchBlock VisitCatchBlock( CatchBlock node )
    {
        EnterExpression( "MakeCatchBlock" );

        AppendIndented( $"typeof({GetTypeString( node.Test )})" );
        Append( ",\n" );

        if ( node.Variable != null )
        {
            Visit( node.Variable );
            Append( ",\n" );
        }
        else
        {
            AppendIndented( "null,\n" );
        }

        Visit( node.Body );
        Append( ",\n" );

        if ( node.Filter != null )
        {
            Visit( node.Filter );
        }
        else
        {
            AppendIndented( "null" );
        }

        ExitExpression();
        return node;
    }

    protected override Expression VisitUnary( UnaryExpression node )
    {
        EnterExpression( $"{node.NodeType}" );

        Visit( node.Operand );

        if ( node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked )
        {
            Append( ",\n" );
            AppendIndented( $"typeof({GetTypeString( node.Type )})" );
        }

        ExitExpression();
        return node;
    }

    private string GetMemberInfoString( MemberInfo memberInfo )
    {
        // TODO: Improve lookup
        return $"typeof({GetTypeString( memberInfo.DeclaringType )}).GetMember(\"{memberInfo.Name}\")[0]";
    }

    private string GetMethodInfoString( MethodInfo methodInfo )
    {
        var methodName = methodInfo.Name;
        var declaringType = GetTypeString( methodInfo.DeclaringType );

        var parameters = methodInfo.GetParameters();
        var parameterTypes = parameters.Length > 0
            ? $"new[] {{ {string.Join( ", ", parameters.Select( p => $"typeof({GetTypeString( p.ParameterType )})" ) )} }}"
            : "Type.EmptyTypes";

        return $"typeof({declaringType}).GetMethod(\"{methodName}\", {parameterTypes})";
    }

    private string GetTypeString( Type type )
    {
        if ( !_usings.Contains( type.Namespace ) )
        {
            _usings.Add( type.Namespace );
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
            int backtickIndex = baseTypeName.IndexOf( '`' );
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

    private void EnterExpression( string name, bool newLine = true )
    {
        AppendIndented( $"{_config.Prefix}{name}(" );
        if ( newLine )
        {
            _expressionOutput.Append( "\n" );
        }
        _depth++;
    }

    private void ExitExpression( bool newLine = true )
    {
        _depth--;
        if ( newLine )
        {
            _expressionOutput.Append( "\n" );
            AppendIndented( ")" );
        }
        else
        {
            _expressionOutput.Append( ")" );
        }
    }

    private void AppendArgumentTypes( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Append( ",\n" );
            else
                Append( "\n" );

            AppendIndented( "new[] {\n" );
            _depth++;
            for ( var i = 0; i < arguments.Count; i++ )
            {
                AppendIndented( $"typeof({GetTypeString( arguments[i].Type )})" );
                if ( i < arguments.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "}" );
        }
    }

    private void AppendArguments( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Append( ",\n" );
            else
                Append( "\n" );

            for ( var i = 0; i < arguments.Count; i++ )
            {
                Visit( arguments[i] );
                if ( i < arguments.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
        }
    }

    private void AppendParamsArguments( ReadOnlyCollection<Expression> arguments, bool firstArgument = false )
    {
        if ( arguments.Count > 0 )
        {
            if ( !firstArgument )
                Append( ",\n" );
            else
                Append( "\n" );

            AppendIndented( "new[] {\n" );
            _depth++;
            for ( var i = 0; i < arguments.Count; i++ )
            {
                Visit( arguments[i] );
                if ( i < arguments.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "}" );
        }
    }
    private void AppendInitializers( ReadOnlyCollection<ElementInit> initializers )
    {
        AppendIndented( "new[] {\n" );
        _depth++;
        for ( var i = 0; i < initializers.Count(); i++ )
        {
            VisitElementInit( initializers[i] );
            if ( i < initializers.Count - 1 )
            {
                Append( "," );
            }
            Append( "\n" );
        }
        _depth--;
        AppendIndented( "}" );
    }

    private void AppendSwitchCases( ReadOnlyCollection<SwitchCase> cases )
    {
        if ( cases.Count > 0 )
        {
            Append( ",\n" );
            AppendIndented( "new[] {\n" );
            _depth++;
            for ( var i = 0; i < cases.Count; i++ )
            {
                VisitSwitchCase( cases[i] );
                if ( i < cases.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "}" );
        }
    }

    private void AppendCatchBlocks( ReadOnlyCollection<CatchBlock> handlers )
    {
        if ( handlers != null && handlers.Count > 0 )
        {
            Append( ",\n" );
            AppendIndented( "new[] {\n" );
            _depth++;
            for ( var i = 0; i < handlers.Count; i++ )
            {
                VisitCatchBlock( handlers[i] );
                if ( i < handlers.Count - 1 )
                {
                    Append( "," );
                }
                Append( "\n" );
            }
            _depth--;
            AppendIndented( "}" );
        }
    }

    private void Append( object value )
    {
        _expressionOutput.Append( value );
    }

    private void AppendIndented( object value )
    {
        _expressionOutput.Append( new string( _config.Indentation, _depth ) );
        _expressionOutput.Append( value );
    }

    private string GenerateUniqueName( string name, Type type )
    {
        // Start with the parameter's name if it exists; otherwise, infer a name from the type
        var baseName = string.IsNullOrEmpty( name )
            ? InferNameFromType( type )
            : name;

        var uniqueName = baseName;
        int counter = 1;
        while ( _parameters.ContainsValue( uniqueName ) || IsKeyword( uniqueName ) )
        {
            uniqueName = $"{baseName}{counter}";
            counter++;
        }

        return uniqueName;
    }

    private string GenerateLabelUniqueName( string name, Type type )
    {
        // Start with the parameter's name if it exists; otherwise, infer a name from the type
        var baseName = string.IsNullOrEmpty( name )
            ? InferNameFromType( type )
            : name;

        var uniqueName = baseName;
        var counter = 1;

        while ( _labels.ContainsValue( uniqueName ) || IsKeyword( uniqueName ) )
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

    private string InferNameFromType( Type type )
    {
        var typeName = type.Name;

        if ( type.IsGenericType )
        {
            int backtickIndex = typeName.IndexOf( '`' );
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
    }

    private static List<string> SplitTypeNameByCasing( ReadOnlySpan<char> typeName )
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
