using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.Core.Writer;

internal class XsVisitor( XsWriterContext context ) : global::System.Linq.Expressions.ExpressionVisitor
{
    protected override Expression VisitBinary( BinaryExpression node )
    {
        using var writer = context.GetWriter();
        var group = GroupBinary( node.NodeType );

        if ( group )
            writer.Write( "(" );

        writer.WriteExpression( node.Left );

        switch ( node.NodeType )
        {
            case ExpressionType.Equal:
                writer.Write( " == " );
                break;
            case ExpressionType.Assign:
                writer.Write( " = " );
                break;
            case ExpressionType.Add:
                writer.Write( " + " );
                break;
            case ExpressionType.Subtract:
                writer.Write( " - " );
                break;
            case ExpressionType.Multiply:
                writer.Write( " * " );
                break;
            case ExpressionType.Divide:
                writer.Write( " / " );
                break;
            case ExpressionType.Modulo:
                writer.Write( " % " );
                break;
            case ExpressionType.Power:
                writer.Write( " ** " );
                break;
            case ExpressionType.LessThan:
                writer.Write( " < " );
                break;
            case ExpressionType.LessThanOrEqual:
                writer.Write( " <= " );
                break;
            case ExpressionType.GreaterThan:
                writer.Write( " > " );
                break;
            case ExpressionType.GreaterThanOrEqual:
                writer.Write( " >= " );
                break;
            case ExpressionType.Coalesce:
                writer.Write( " ?? " );
                break;
            case ExpressionType.AndAlso:
                writer.Write( " && " );
                break;
            case ExpressionType.And:
                writer.Write( " & " );
                break;
            case ExpressionType.Or:
                writer.Write( " | " );
                break;
            case ExpressionType.RightShift:
                writer.Write( " >> " );
                break;
            case ExpressionType.LeftShift:
                writer.Write( " << " );
                break;
            case ExpressionType.ExclusiveOr:
                writer.Write( " ^ " );
                break;
            case ExpressionType.AddAssign:
            case ExpressionType.AddAssignChecked:
                writer.Write( " += " );
                break;
            case ExpressionType.SubtractAssign:
            case ExpressionType.SubtractAssignChecked:
                writer.Write( " -= " );
                break;
            case ExpressionType.MultiplyAssign:
            case ExpressionType.MultiplyAssignChecked:
                writer.Write( " *= " );
                break;
            case ExpressionType.DivideAssign:
                writer.Write( " /= " );
                break;
            case ExpressionType.ModuloAssign:
                writer.Write( " %= " );
                break;
            case ExpressionType.PowerAssign:
                writer.Write( " **= " );
                break;
            case ExpressionType.AndAssign:
                writer.Write( " &= " );
                break;
            case ExpressionType.OrAssign:
                writer.Write( " |= " );
                break;
            case ExpressionType.RightShiftAssign:
                writer.Write( " >>= " );
                break;
            case ExpressionType.LeftShiftAssign:
                writer.Write( " <<= " );
                break;
            case ExpressionType.ExclusiveOrAssign:
                writer.Write( " ^= " );
                break;
        }

        writer.WriteExpression( node.Right );

        if ( group )
            writer.Write( ")" );

        return node;

        static bool GroupBinary( ExpressionType nodeType )
        {
            return nodeType == ExpressionType.Divide ||
                nodeType == ExpressionType.Multiply ||
                nodeType == ExpressionType.Power;
        }
    }

    protected override Expression VisitBlock( BlockExpression node )
    {
        using var writer = context.GetWriter();

        var count = node.Expressions.Count;
        if ( count == 1 && !context.ForceBlock )
        {
            writer.WriteExpression( node.Expressions[0] );
            return node;
        }

        context.ForceBlock = false;

        writer.Write( "{\n", indent: true );
        writer.Indent();

        for ( var i = 0; i < count; i++ )
        {
            writer.WriteExpression( node.Expressions[i] );
            writer.WriteTerminated();
        }

        writer.Outdent();
        writer.Write( "}\n", indent: true );
        context.SkipTerminated = true;

        return node;
    }

    protected override Expression VisitConditional( ConditionalExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "if(" );//, indent: true );
        writer.WriteExpression( node.Test );
        writer.Write( ") " );

        writer.WriteExpression( node.IfTrue );

        if ( node.IfTrue is not BlockExpression )
            writer.WriteTerminated();

        if ( node.IfFalse != null && node.IfFalse.NodeType != ExpressionType.Default )
        {
            writer.Write( " else " );
            writer.WriteExpression( node.IfFalse );

            if ( node.IfFalse is not BlockExpression )
                writer.WriteTerminated();
        }

        return node;
    }

    protected override Expression VisitConstant( ConstantExpression node )
    {
        using var writer = context.GetWriter();

        var value = node.Value;

        switch ( value )
        {
            case char:
                writer.Write( $"'{value}'" );
                break;

            case string:
                writer.Write( $"\"{value}\"" );
                break;

            case bool boolValue:
                writer.Write( boolValue ? "true" : "false" );
                break;

            case null:
                writer.Write( "null" );
                break;

            default:
                writer.Write( value );
                break;
        }

        return node;
    }

    protected override Expression VisitDefault( DefaultExpression node )
    {
        using var writer = context.GetWriter();

        if ( node.Type != typeof( void ) )
        {
            writer.Write( "default(" );
            writer.WriteType( node.Type );
            writer.Write( ")" );
        }
        else
        {
            writer.Write( "null" );
        }

        return node;
    }

    protected override ElementInit VisitElementInit( ElementInit node )
    {
        using var writer = context.GetWriter();

        var count = node.Arguments.Count;

        for ( var i = 0; i < count; i++ )
        {
            var argument = node.Arguments[i];

            writer.WriteExpression( argument );

            if ( i < count - 1 )
                writer.Write( "," );

        }

        return node;
    }

    protected override Expression VisitExtension( Expression node )
    {
        if ( context.ExtensionWriters is not null )
        {
            foreach ( var writer in context.ExtensionWriters )
            {
                if ( !writer.CanWrite( node ) )
                    continue;

                writer.WriteExpression( node, context );
                return node;
            }
        }

        Visit( node.Reduce() );

        return node;
    }

    protected override Expression VisitGoto( GotoExpression node )
    {
        var writer = context.GetWriter();

        switch ( node.Kind )
        {
            case GotoExpressionKind.Break:
                writer.Write( "break;", indent: true );
                context.SkipTerminated = true;
                break;

            case GotoExpressionKind.Continue:
                writer.Write( "continue;", indent: true );
                context.SkipTerminated = true;
                break;

            case GotoExpressionKind.Goto:
                writer.Write( $"goto {node.Target.Name};", indent: true );
                context.SkipTerminated = true;
                break;

            case GotoExpressionKind.Return:
                writer.Write( "return", indent: true );
                if ( node.Value != null )
                {
                    writer.Write( " " );
                    writer.WriteExpression( node.Value );
                }
                writer.Write( ";" );
                context.SkipTerminated = true;
                break;

            default:
                writer.Write( node.Kind.ToString() );  // TODO: Handle other cases
                break;
        }

        return node;
    }

    protected override Expression VisitIndex( IndexExpression node )
    {
        using var writer = context.GetWriter();

        writer.WriteExpression( node.Object );
        writer.Write( "[" );

        var count = node.Arguments.Count;

        for ( var i = 0; i < count; i++ )
        {
            var argument = node.Arguments[i];

            writer.WriteExpression( argument );
            if ( i < count - 1 )
                writer.Write( "," );
        }

        writer.Write( "]" );

        return node;

    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        using var writer = context.GetWriter();

        writer.WriteExpression( node.Expression );

        writer.Write( "(" );

        var count = node.Arguments.Count;

        for ( var i = 0; i < count; i++ )
        {
            var argument = node.Arguments[i];

            writer.WriteExpression( argument );
            if ( i < count - 1 )
                writer.Write( "," );
        }

        writer.Write( ")" );


        return node;
    }

    protected override Expression VisitLambda<T>( Expression<T> node )
    {
        using var writer = context.GetWriter();

        writer.Write( "(" );
        var count = node.Parameters.Count;

        for ( var i = 0; i < count; i++ )
        {
            var parameter = node.Parameters[i];
            context.Parameters.Add( parameter, parameter.Name );

            writer.WriteType( parameter.Type );
            writer.Write( $" {parameter.Name}" );
            if ( i < count - 1 )
            {
                writer.Write( ", " );
            }
        }

        writer.Write( ") => " );

        writer.WriteExpression( node.Body );

        return node;
    }

    protected override Expression VisitListInit( ListInitExpression node )
    {
        using var writer = context.GetWriter();

        writer.WriteExpression( node.NewExpression );

        writer.Write( "{\n", indent: true );
        writer.Indent();

        VisitInitializers( node.Initializers );

        writer.Outdent();
        writer.Write( "}\n", indent: true );
        context.SkipTerminated = true;

        return node;
    }

    protected override Expression VisitMember( MemberExpression node )
    {
        using var writer = context.GetWriter();

        writer.WriteExpression( node.Expression );
        writer.Write( "." );

        writer.WriteMemberInfo( node.Member );

        return node;
    }

    protected override Expression VisitMethodCall( MethodCallExpression node )
    {
        using var writer = context.GetWriter();

        var startArgument = 0;
        if ( node.Method.IsDefined( typeof( ExtensionAttribute ), inherit: false ) )
        {
            writer.WriteExpression( node.Arguments[0] );
            startArgument = 1;
        }
        else if ( node.Object != null )
        {
            writer.WriteExpression( node.Object );
        }
        else
        {
            writer.WriteType( node.Method.DeclaringType );
        }

        writer.Write( "." );
        writer.WriteMethodInfo( node.Method );

        writer.Write( "(" );

        var count = node.Arguments.Count;

        for ( var i = startArgument; i < count; i++ )
        {
            var argument = node.Arguments[i];

            if ( argument is ConstantExpression constValue && constValue.Value == null )
            {
                writer.Write( "default(" );
                writer.WriteType( constValue.Type );
                writer.Write( ")" );
            }
            else
            {
                writer.WriteExpression( argument );
            }

            if ( i < count - 1 )
                writer.Write( "," );
        }

        writer.Write( ")" );

        return node;
    }

    protected override Expression VisitNew( NewExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "new " );

        writer.WriteConstructorInfo( node.Constructor );

        writer.Write( "(" );

        var count = node.Arguments.Count;

        for ( var i = 0; i < count; i++ )
        {
            var argument = node.Arguments[i];

            writer.WriteExpression( argument );
            if ( i < count - 1 )
                writer.Write( "," );
        }

        writer.Write( ")" );

        return node;
    }

    protected override Expression VisitNewArray( NewArrayExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "new " );

        writer.WriteType( node.NodeType == ExpressionType.NewArrayBounds
            ? node.Type
            : node.Type.GetElementType()
        );

        writer.Write( "[] {\n", indent: true );
        writer.Indent();

        var count = node.Expressions.Count;
        for ( var i = 0; i < count; i++ )
        {
            writer.WriteExpression( node.Expressions[i] );

            if ( i < count - 1 )
                writer.Write( ", " );
        }

        writer.Outdent();
        writer.Write( "}\n", indent: true );
        context.SkipTerminated = true;

        return node;
    }

    protected override Expression VisitParameter( ParameterExpression node )
    {
        var writer = context.GetWriter();
        writer.WriteParameter( node );
        return node;
    }

    protected override Expression VisitLabel( LabelExpression node )
    {
        using var writer = context.GetWriter();

        if ( node.Target.Name != "ReturnLabel" )
            writer.Write( $"{node.Target.Name}:\n" );

        context.SkipTerminated = true;
        return node;
    }

    protected override LabelTarget VisitLabelTarget( LabelTarget node )
    {
        using var writer = context.GetWriter();

        writer.Write( $"{node.Name}:\n" );
        context.SkipTerminated = true;

        return node;
    }

    protected override Expression VisitLoop( LoopExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "loop", indent: true );

        writer.WriteExpression( node.Body );

        return node;
    }

    protected override Expression VisitSwitch( SwitchExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "switch (" );

        writer.WriteExpression( node.SwitchValue );

        writer.Write( ") {\n" );

        if ( node.Cases.Count > 0 )
        {
            VisitSwitchCases( node.Cases );
        }

        if ( node.DefaultBody != null )
        {
            writer.Write( "default:\n" );
            writer.WriteExpression( node.DefaultBody );
        }

        writer.Write( "}\n" );
        context.SkipTerminated = true;

        return node;
    }

    protected override SwitchCase VisitSwitchCase( SwitchCase node )
    {
        using var writer = context.GetWriter();
        writer.Write( "case " );

        writer.WriteExpressions( node.TestValues );
        writer.Write( ":\n" );

        writer.WriteExpression( node.Body );
        context.SkipTerminated = false;

        return node;
    }

    protected override Expression VisitTry( TryExpression node )
    {
        using var writer = context.GetWriter();

        writer.Write( "try\n", indent: true );

        context.ForceBlock = true;
        writer.WriteExpression( node.Body );

        VisitCatchBlocks( node.Handlers );

        if ( node.Finally != null )
        {
            context.SkipTerminated = false;
            context.ForceBlock = true;
            writer.Write( "finally\n", indent: true );
            writer.WriteExpression( node.Finally );
        }

        return node;
    }

    protected override Expression VisitTypeBinary( TypeBinaryExpression node )
    {
        using var writer = context.GetWriter();
        writer.WriteExpression( node.Expression );
        writer.Write( " is " );
        writer.WriteType( node.TypeOperand );
        return node;
    }
    protected override CatchBlock VisitCatchBlock( CatchBlock node )
    {
        using var writer = context.GetWriter();

        writer.Write( "catch (", indent: true );

        writer.WriteType( node.Test );

        var exception = node.Variable;

        if ( exception != null )
        {
            if ( exception.Name != null )
                writer.Write( $" {exception.Name}" );
        }

        writer.Write( ")\n" );

        context.ForceBlock = true;
        writer.WriteExpression( node.Body );
        writer.Write( "\n" );

        // TODO: Handle Filter

        return node;
    }

    protected override Expression VisitUnary( UnaryExpression node )
    {
        using var writer = context.GetWriter();

        switch ( node.NodeType )
        {
            case ExpressionType.PreIncrementAssign:
                writer.Write( "++" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.PreDecrementAssign:
                writer.Write( "--" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.PostIncrementAssign:
                writer.WriteExpression( node.Operand );
                writer.Write( "++" );
                break;

            case ExpressionType.PostDecrementAssign:
                writer.WriteExpression( node.Operand );
                writer.Write( "--" );
                break;

            case ExpressionType.Not:
                writer.Write( "!" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.Negate:
                writer.Write( "-" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.OnesComplement:
                writer.Write( "~" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.IsFalse:
                writer.Write( "!?" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.IsTrue:
                writer.Write( "?" );
                writer.WriteExpression( node.Operand );
                break;

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                writer.WriteExpression( node.Operand );
                writer.Write( " as " );
                writer.WriteType( node.Type );
                break;

            case ExpressionType.TypeAs:
                writer.WriteExpression( node.Operand );
                writer.Write( " as? " );
                writer.WriteType( node.Type );
                break;
        }

        return node;
    }

    private void VisitInitializers( ReadOnlyCollection<ElementInit> initializers )
    {
        var writer = context.GetWriter();
        writer.Indent();

        var count = initializers.Count;

        for ( var i = 0; i < count; i++ )
        {
            VisitElementInit( initializers[i] );
            if ( i < count - 1 )
            {
                writer.Write( ", " );
            }
        }

        writer.Outdent();
    }

    private void VisitSwitchCases( ReadOnlyCollection<SwitchCase> cases )
    {
        if ( cases.Count <= 0 )
        {
            return;
        }

        var writer = context.GetWriter();
        writer.Indent();

        var count = cases.Count;

        for ( var i = 0; i < count; i++ )
        {
            VisitSwitchCase( cases[i] );
            writer.Write( "\n" );
        }

        writer.Outdent();
    }

    private void VisitCatchBlocks( ReadOnlyCollection<CatchBlock> handlers )
    {
        if ( handlers == null || handlers.Count <= 0 )
        {
            return;
        }

        var writer = context.GetWriter();
        writer.Indent();

        var count = handlers.Count;

        for ( var i = 0; i < count; i++ )
        {
            context.SkipTerminated = false;

            VisitCatchBlock( handlers[i] );
            writer.Write( "\n" );
        }

        writer.Outdent();
    }

}
