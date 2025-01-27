﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

internal class ExpressionTreeVisitor( ExpressionWriterContext Context ) : ExpressionVisitor
{
    protected override Expression VisitBinary( BinaryExpression node )
    {
        using var writer = Context.EnterExpression( $"{node.NodeType}" );

        writer.WriteExpression( node.Left );
        writer.Write( ",\n" );

        writer.WriteExpression( node.Right );

        if ( node.Method != null )
        {
            writer.Write( ",\n" );
            writer.WriteMethodInfo( node.Method );
        }

        if ( node.Conversion != null )
        {
            writer.Write( ",\n" );
            writer.WriteExpression( node.Conversion );
        }

        return node;
    }

    protected override Expression VisitBlock( BlockExpression node )
    {
        using var writer = Context.EnterExpression( "Block" );

        writer.WriteParamExpressions( node.Variables, true );

        if ( node.Variables.Count > 0 )
        {
            writer.Write( ",\n" );
        }

        for ( var i = 0; i < node.Expressions.Count; i++ )
        {
            writer.WriteExpression( node.Expressions[i] );
            if ( i < node.Expressions.Count - 1 )
            {
                writer.Write( ",\n" );
            }
        }

        return node;
    }

    protected override Expression VisitConditional( ConditionalExpression node )
    {
        using var writer = Context.EnterExpression( "Condition" );

        writer.WriteExpression( node.Test );
        writer.Write( ",\n" );

        writer.WriteExpression( node.IfTrue );
        writer.Write( ",\n" );

        writer.WriteExpression( node.IfFalse );
        writer.Write( ",\n" );

        writer.WriteType( node.Type );

        return node;
    }

    protected override Expression VisitConstant( ConstantExpression node )
    {
        using var writer = Context.EnterExpression( "Constant", newLine: false );
        var value = node.Value;

        switch ( value )
        {
            case string:
                writer.Write( $"\"{value}\"" );
                break;

            case bool boolValue:
                writer.Write( boolValue ? "true" : "false" );
                break;

            case null:
                writer.Write( $"null, typeof({writer.GetTypeString( node.Type )})" );
                break;

            default:
                writer.Write( value );
                break;
        }

        return node;
    }

    protected override Expression VisitDefault( DefaultExpression node )
    {
        if ( node.Type != null && node.Type != typeof( void ) )
        {
            using var writer = Context.EnterExpression( "Default" );
            writer.WriteType( node.Type );
        }
        else
        {
            using var writer = Context.EnterExpression( "Empty", newLine: false );
        }

        return node;
    }

    protected override ElementInit VisitElementInit( ElementInit node )
    {
        using var writer = Context.EnterExpression( "ElementInit" );

        writer.WriteMethodInfo( node.AddMethod );
        writer.WriteExpressions( node.Arguments );

        return node;
    }

    protected override Expression VisitExtension( Expression node )
    {
        if ( Context.ExtensionWriters is not null )
        {
            foreach ( var write in Context.ExtensionWriters )
            {
                if ( write.TryExpressionWriter( node, Context ) )
                    return node;
            }
        }

        Visit( node.Reduce() );

        return node;
    }

    protected override Expression VisitGoto( GotoExpression node )
    {
        using var writer = Context.EnterExpression( $"{node.Kind}" );

        if ( Context.Labels.TryGetValue( node.Target, out var lableTarget ) )
        {
            writer.Write( lableTarget, indent: true );
        }
        else
        {
            VisitLabelTarget( node.Target );
            Context.Labels.TryGetValue( node.Target, out lableTarget );
            writer.Write( lableTarget, indent: true );
        }

        if ( node.Value != null )
        {
            writer.Write( ",\n" );
            writer.WriteExpression( node.Value );
        }

        if ( node.Type != null && node.NodeType == ExpressionType.Default && node.Type != typeof( void ) )
        {
            writer.Write( ",\n" );
            writer.WriteType( node.Type );
        }

        return node;
    }

    protected override Expression VisitIndex( IndexExpression node )
    {
        if ( node.Indexer != null )
        {
            using var writer = Context.EnterExpression( "MakeIndex" );

            writer.WriteExpression( node.Object );
            writer.Write( ",\n" );

            //writer.WriteType( node.Type );
            writer.Write( $"typeof({writer.GetTypeString( node.Indexer.DeclaringType )}).GetProperty(\"{node.Indexer.Name}\", \n", indent: true );

            writer.Indent();
            writer.Write( "new[] {\n", indent: true );

            writer.Indent();
            var parameters = node.Indexer.GetIndexParameters();

            for ( var i = 0; i < parameters.Length; i++ )
            {
                writer.WriteType( parameters[i].ParameterType );
                if ( i < parameters.Length - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }

            writer.Outdent();
            writer.Write( "}\n", indent: true );

            writer.Outdent();
            writer.Write( $")", indent: true );

            writer.WriteParamExpressions( node.Arguments );
        }
        else
        {
            using var writer = Context.EnterExpression( "ArrayAccess" );

            writer.WriteExpression( node.Object );

            writer.WriteExpressions( node.Arguments );
        }

        return node;
    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        using var writer = Context.EnterExpression( "Invoke" );

        writer.WriteExpression( node.Expression );

        writer.WriteExpressions( node.Arguments );

        return node;
    }

    protected override Expression VisitLambda<T>( Expression<T> node )
    {
        using var writer = Context.EnterExpression( "Lambda" );

        writer.WriteExpression( node.Body );

        if ( node.Parameters.Count > 0 )
        {
            writer.Write( ",\n" );
            writer.Write( "new[] {\n", indent: true );

            writer.Indent();

            var count = node.Parameters.Count;

            for ( var i = 0; i < count; i++ )
            {
                writer.WriteExpression( node.Parameters[i] );
                if ( i < count - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }

            writer.Outdent();
            writer.Write( "}", indent: true );
        }

        return node;
    }

    protected override Expression VisitListInit( ListInitExpression node )
    {
        using var writer = Context.EnterExpression( "ListInit" );

        writer.WriteExpression( node.NewExpression );
        writer.Write( ",\n" );

        VisitInitializers( node.Initializers );

        return node;
    }

    protected override Expression VisitMember( MemberExpression node )
    {
        using var writer = Context.EnterExpression( "MakeMemberAccess" );

        writer.WriteExpression( node.Expression );
        writer.Write( $",\n" );

        writer.WriteMemberInfo( node.Member );

        return node;
    }

    protected override Expression VisitMethodCall( MethodCallExpression node )
    {
        using var writer = Context.EnterExpression( "Call" );

        if ( node.Object != null )
        {
            writer.WriteExpression( node.Object );
            writer.Write( ",\n" );
        }
        else
        {
            writer.Write( $"null,\n", indent: true );
        }

        writer.WriteMethodInfo( node.Method );

        writer.WriteExpressions( node.Arguments );

        return node;
    }

    protected override Expression VisitNew( NewExpression node )
    {
        using var writer = Context.EnterExpression( "New" );

        writer.WriteConstructorInfo( node.Constructor );
        writer.WriteExpressions( node.Arguments );

        return node;
    }

    protected override Expression VisitNewArray( NewArrayExpression node )
    {
        using var writer = Context.EnterExpression( $"{node.NodeType}" );

        if ( node.NodeType == ExpressionType.NewArrayBounds )
            writer.WriteType( node.Type );
        else
            writer.WriteType( node.Type.GetElementType() );

        writer.WriteExpressions( node.Expressions );

        return node;
    }

    protected override Expression VisitParameter( ParameterExpression node )
    {
        var writer = Context.GetWriter();
        writer.WriteParameter( node );
        return node;
    }

    protected override Expression VisitLabel( LabelExpression node )
    {
        using var writer = Context.EnterExpression( "Label" );

        if ( Context.Labels.TryGetValue( node.Target, out var lableTarget ) )
        {
            writer.Write( lableTarget, indent: true );
        }
        else
        {
            VisitLabelTarget( node.Target );
            Context.Labels.TryGetValue( node.Target, out lableTarget );
            writer.Write( lableTarget, indent: true );
        }

        if ( node.DefaultValue != null )
        {
            writer.Write( ",\n" );
            writer.WriteExpression( node.DefaultValue );
        }

        return node;
    }

    protected override LabelTarget VisitLabelTarget( LabelTarget node )
    {
        Context.GetWriter()
            .WriteLabel( node );

        return node;
    }

    protected override Expression VisitLoop( LoopExpression node )
    {
        using var writer = Context.EnterExpression( "Loop" );

        writer.WriteExpression( node.Body );

        if ( node.BreakLabel != null )
        {
            VisitLabelTarget( node.BreakLabel );
            if ( Context.Labels.TryGetValue( node.BreakLabel, out var breakLabel ) )
            {
                writer.Write( ",\n" );
                writer.Write( breakLabel, indent: true );
            }
        }

        if ( node.ContinueLabel != null )
        {
            VisitLabelTarget( node.ContinueLabel );
            if ( Context.Labels.TryGetValue( node.ContinueLabel, out var continueLabel ) )
            {
                writer.Write( ",\n" );
                writer.Write( continueLabel, indent: true );
            }
        }

        return node;
    }

    protected override Expression VisitSwitch( SwitchExpression node )
    {
        using var writer = Context.EnterExpression( "Switch" );

        writer.WriteExpression( node.SwitchValue );
        if ( node.DefaultBody != null )
        {
            writer.Write( ",\n" );
            writer.WriteExpression( node.DefaultBody );
        }

        if ( node.Cases != null && node.Cases.Count > 0 )
        {
            VisitSwitchCases( node.Cases );
        }

        return node;
    }

    protected override SwitchCase VisitSwitchCase( SwitchCase node )
    {
        using var writer = Context.EnterExpression( "SwitchCase" );

        writer.WriteExpression( node.Body );
        writer.WriteExpressions( node.TestValues );

        return node;
    }

    protected override Expression VisitTry( TryExpression node )
    {
        using var writer = Context.EnterExpression( "TryCatchFinally" );

        writer.WriteExpression( node.Body );
        writer.Write( ",\n" );

        if ( node.Finally != null )
        {
            writer.WriteExpression( node.Finally );
        }
        else
        {
            writer.Write( "null", indent: true );
        }

        VisitCatchBlocks( node.Handlers );

        return node;
    }

    protected override CatchBlock VisitCatchBlock( CatchBlock node )
    {
        using var writer = Context.EnterExpression( "MakeCatchBlock" );

        writer.WriteType( node.Test );
        writer.Write( ",\n" );

        if ( node.Variable != null )
        {
            writer.WriteExpression( node.Variable );
            writer.Write( ",\n" );
        }
        else
        {
            writer.Write( "null,\n", indent: true );
        }

        writer.WriteExpression( node.Body );
        writer.Write( ",\n" );

        if ( node.Filter != null )
        {
            writer.WriteExpression( node.Filter );
        }
        else
        {
            writer.Write( "null", indent: true );
        }

        return node;
    }

    protected override Expression VisitUnary( UnaryExpression node )
    {
        using var writer = Context.EnterExpression( $"{node.NodeType}" );

        writer.WriteExpression( node.Operand );

        if ( node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked )
        {
            writer.Write( ",\n" );
            writer.WriteType( node.Type );
        }

        return node;
    }

    private void VisitInitializers( ReadOnlyCollection<ElementInit> initializers )
    {
        var writer = Context.GetWriter();

        writer.Write( "new[] {\n", indent: true );
        writer.Indent();

        var count = initializers.Count;

        for ( var i = 0; i < count; i++ )
        {
            VisitElementInit( initializers[i] );

            if ( i < count - 1 )
            {
                writer.Write( "," );
            }
            writer.Write( "\n" );
        }

        writer.Outdent();
        writer.Write( "}", indent: true );
    }

    private void VisitSwitchCases( ReadOnlyCollection<SwitchCase> cases )
    {
        if ( cases.Count > 0 )
        {
            var writer = Context.GetWriter();

            writer.Write( ",\n" );
            writer.Write( "new[] {\n", indent: true );
            writer.Indent();

            var count = cases.Count;

            for ( var i = 0; i < count; i++ )
            {
                VisitSwitchCase( cases[i] );

                if ( i < count - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }

            writer.Outdent();
            writer.Write( "}", indent: true );
        }
    }

    private void VisitCatchBlocks( ReadOnlyCollection<CatchBlock> handlers )
    {
        if ( handlers != null && handlers.Count > 0 )
        {
            var writer = Context.GetWriter();

            writer.Write( ",\n" );
            writer.Write( "new[] {\n", indent: true );
            writer.Indent();

            var count = handlers.Count;

            for ( var i = 0; i < count; i++ )
            {
                VisitCatchBlock( handlers[i] );
                if ( i < count - 1 )
                {
                    writer.Write( "," );
                }
                writer.Write( "\n" );
            }

            writer.Outdent();
            writer.Write( "}", indent: true );
        }
    }

}
