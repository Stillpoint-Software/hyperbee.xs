using System.Linq.Expressions;
using System.Numerics;
using Hyperbee.Collections;
using Hyperbee.XS.Core;

namespace Hyperbee.XS.Interpreter;

public class InterpretScope : ParseScope
{
    public LinkedDictionary<ParameterExpression, object> Values { get; } = new();

    public override void EnterScope(FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null)
    {
        base.EnterScope(frameType, breakLabel, continueLabel);
        Values.Push();
    }

    public override void ExitScope()
    {
        Values.Pop();
        base.ExitScope();
    }
}

public sealed class XsInterpreter : ExpressionVisitor
{
    private readonly InterpretScope _scope;
    private readonly XsDebugger _debugger;
    private readonly Stack<object> _resultStack = new();

    public XsInterpreter( XsDebugger debugger = null )
    {
        _scope = new InterpretScope();
        _debugger = debugger;
    }

    public Func<T> Interpreter<T>( LambdaExpression expression )
    {
        return () => (T) Evaluate( expression );
    }

    private object Evaluate( Expression expression )
    {
        Visit( expression );
        return _resultStack.Pop();
    }

    protected override Expression VisitConstant( ConstantExpression node )
    {
        _resultStack.Push( node.Value! );
        return node;
    }

    protected override Expression VisitParameter( ParameterExpression node )
    {
        if ( !_scope.Values.TryGetValue( node, out var value ) )
            throw new InterpreterException( $"Parameter '{node.Name}' not found.", node );

        _resultStack.Push( value );
        return node;
    }

    protected override Expression VisitBinary( BinaryExpression node )
    {
        Visit( node.Left );
        var left = _resultStack.Pop();

        Visit( node.Right );
        var right = _resultStack.Pop();

        _resultStack.Push( EvaluateBinary( node, left, right ) );
        return node;
    }

    protected override Expression VisitUnary( UnaryExpression node )
    {
        Visit( node.Operand );
        var operand = _resultStack.Pop();

        _resultStack.Push( EvaluateUnary( node, operand ) );
        return node;
    }

    protected override Expression VisitConditional( ConditionalExpression node )
    {
        Visit( node.Test );
        var condition = (bool) _resultStack.Pop();

        Visit( condition ? node.IfTrue : node.IfFalse );
        return node;
    }

    protected override Expression VisitBlock( BlockExpression node )
    {
        _scope.EnterScope( FrameType.Block );

        try
        {
            foreach ( var variable in node.Variables )
            {
                _scope.Variables[variable.Name!] = variable;
                _scope.Values[variable] = AssignDefault( variable.Type ); 
            }

            foreach ( var expression in node.Expressions )
                Visit( expression );
        }
        finally
        {
            _scope.ExitScope();
        }

        return node;

        static object AssignDefault( Type type )
        {
            if ( type == typeof(string) )
                return string.Empty;

            return type.IsValueType 
                ? Activator.CreateInstance( type ) : // default
                null;
        }
    }

    protected override Expression VisitSwitch( SwitchExpression node )
    {
        Visit( node.SwitchValue );
        var switchValue = _resultStack.Pop(); 

        foreach ( var switchCase in node.Cases )
        {
            foreach ( var testValue in switchCase.TestValues )
            {
                Visit( testValue );
                var testResult = _resultStack.Pop();

                if ( !Equals( switchValue, testResult ) )
                    continue;

                Visit( switchCase.Body );
                return node;
            }
        }

        if ( node.DefaultBody != null )
        {
            Visit( node.DefaultBody );
        }

        return node;
    }

    protected override Expression VisitLambda<T>( Expression<T> node )
    {
        _scope.EnterScope( FrameType.Method );

        try
        {
            var args = new object[node.Parameters.Count];

            for ( var i = 0; i < node.Parameters.Count; i++ )
            {
                Visit( node.Parameters[i] ); 
                args[i] = _resultStack.Pop(); 
                _scope.Values[node.Parameters[i]] = args[i]; 
            }

            Visit( node.Body );
            return node;
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        Visit( node.Expression );

        if ( _resultStack.Pop() is not Delegate delegateInstance )
            throw new InterpreterException( "Invocation target is not a valid delegate.", node );

        var arguments = new object[node.Arguments.Count];
        for ( var i = 0; i < node.Arguments.Count; i++ )
        {
            Visit( node.Arguments[i] );
            arguments[i] = _resultStack.Pop();
        }

        var result = delegateInstance.DynamicInvoke( arguments );

        _resultStack.Push( result );
        return node;
    }

    private object EvaluateBinary( BinaryExpression binary, object left, object right )
    {
        // Variable assignments

        if ( binary.NodeType == ExpressionType.Assign )
        {
            if ( binary.Left is not ParameterExpression variable )
                throw new InterpreterException( "Left side of assignment must be a variable.", binary );

            _scope.Values[variable] = right;
            return right;
        }

        // Handle type widening

        var leftType = left.GetType();
        var rightType = right.GetType();
        var widenedType = GetWidenedType( leftType, rightType );

        // Logical comparisons

        if ( binary.NodeType is ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.LessThan
            or ExpressionType.GreaterThan
            or ExpressionType.LessThanOrEqual
            or ExpressionType.GreaterThanOrEqual )
        {
            return widenedType switch
            {
                Type when widenedType == typeof(int) => EvaluateLogicalBinary( binary, (int) left!, (int) right! ),
                Type when widenedType == typeof(long) => EvaluateLogicalBinary( binary, (long) left!, (long) right! ),
                Type when widenedType == typeof(float) => EvaluateLogicalBinary( binary, (float) left!, (float) right! ),
                Type when widenedType == typeof(double) => EvaluateLogicalBinary( binary, (double) left!, (double) right! ),
                Type when widenedType == typeof(decimal) => EvaluateLogicalBinary( binary, (decimal) left!, (decimal) right! ),
                _ => throw new InterpreterException( $"Unsupported logical operation: {binary.NodeType}", binary )
            };
        }

        // Arithmetic operations

        return widenedType switch
        {
            Type when widenedType == typeof(int) => EvaluateArithmeticBinary( binary, (int) left!, (int) right! ),
            Type when widenedType == typeof(long) => EvaluateArithmeticBinary( binary, (long) left!, (long) right! ),
            Type when widenedType == typeof(float) => EvaluateArithmeticBinary( binary, (float) left!, (float) right! ),
            Type when widenedType == typeof(double) => EvaluateArithmeticBinary( binary, (double) left!, (double) right! ),
            Type when widenedType == typeof(decimal) => EvaluateArithmeticBinary( binary, (decimal) left!, (decimal) right! ),
            _ => throw new InterpreterException( $"Unsupported binary operation: {binary.NodeType}", binary )
        };
    }

    private static bool EvaluateLogicalBinary<T>( BinaryExpression binary, T left, T right )
        where T : IComparable<T>
    {
        return binary.NodeType switch
        {
            ExpressionType.LessThan => left.CompareTo( right ) < 0,
            ExpressionType.GreaterThan => left.CompareTo( right ) > 0,
            ExpressionType.LessThanOrEqual => left.CompareTo( right ) <= 0,
            ExpressionType.GreaterThanOrEqual => left.CompareTo( right ) >= 0,
            ExpressionType.Equal => left.Equals( right ),
            ExpressionType.NotEqual => !left.Equals( right ),
            _ => throw new InterpreterException( $"Unsupported logical operation: {binary.NodeType}", binary )
        };
    }

    private static T EvaluateArithmeticBinary<T>( BinaryExpression binary, T left, T right )
        where T : INumber<T>
    {
        return binary.NodeType switch
        {
            ExpressionType.Add => left + right,
            ExpressionType.Subtract => left - right,
            ExpressionType.Multiply => left * right,
            ExpressionType.Divide => left / right,
            ExpressionType.Modulo => left % right,
            ExpressionType.Power => T.CreateChecked(Math.Pow( double.CreateChecked( left ), double.CreateChecked( right ) )),
            _ => throw new InterpreterException( $"Unsupported arithmetic operation: {binary.NodeType}", binary )
        };
    }

    private static object EvaluateUnary( UnaryExpression unary, object operand )
    {
        if ( unary.NodeType == ExpressionType.Convert )
        {
            if ( operand is not IConvertible convertible )
                throw new InterpreterException( $"Cannot convert {operand.GetType()} to {unary.Type}", unary );

            return Convert.ChangeType( convertible, unary.Type );
        }

        var operandType = operand.GetType();

        return operandType switch
        {
            Type when operandType == typeof( int ) => EvaluateNumericUnary( unary, (int) operand ),
            Type when operandType == typeof( long ) => EvaluateNumericUnary( unary, (long) operand ),
            Type when operandType == typeof( float ) => EvaluateNumericUnary( unary, (float) operand ),
            Type when operandType == typeof( double ) => EvaluateNumericUnary( unary, (double) operand ),
            Type when operandType == typeof( decimal ) => EvaluateNumericUnary( unary, (decimal) operand ),
            Type when operandType == typeof( bool ) => EvaluateLogicalUnary( unary, (bool) operand ),
            _ => throw new InterpreterException( $"Unsupported unary operation for type {operandType}", unary )
        };
    }

    private static object EvaluateNumericUnary<T>( UnaryExpression unary, T operand )
        where T : INumber<T>
    {
        return unary.NodeType switch
        {
            ExpressionType.Negate => -operand,
            _ => throw new InterpreterException( $"Unsupported numeric unary operation: {unary.NodeType}", unary )
        };
    }

    private static bool EvaluateLogicalUnary( UnaryExpression unary, bool operand )
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => !operand,
            ExpressionType.IsFalse => !operand,
            ExpressionType.IsTrue => operand,
            _ => throw new InterpreterException( $"Unsupported boolean unary operation: {unary.NodeType}", unary )
        };
    }

    private static Type GetWidenedType( Type leftType, Type rightType )
    {
        if ( leftType == rightType )
            return leftType;

        if ( TypeResolver.IsWideningConversion( leftType, rightType ) )
            return rightType;

        if ( TypeResolver.IsWideningConversion( rightType, leftType ) )
            return leftType;

        throw new InvalidOperationException( $"No valid widening conversion between {leftType} and {rightType}." );
    }
}
