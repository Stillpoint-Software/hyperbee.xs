﻿using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

internal readonly struct ControlFrame
{
    public ControlFrameType Type { get; }
    public GotoExpression GotoExpr { get; }
    public LambdaExpression LambdaExpr { get; }
    public LabelTarget TargetLabel { get; }
    public object State { get; }

    private ControlFrame( ControlFrameType type, GotoExpression gotoExpr, object state = null )
    {
        Type = type;
        GotoExpr = gotoExpr;
        State = state;
    }

    private ControlFrame( ControlFrameType type, LabelTarget targetLabel, object state = null )
    {
        Type = type;
        TargetLabel = targetLabel;
        State = state;
    }

    private ControlFrame( ControlFrameType type, LambdaExpression lambdaExpr, Dictionary<ParameterExpression, object> capturedScope )
    {
        Type = type;
        LambdaExpr = lambdaExpr;
        State = capturedScope;
    }

    public static ControlFrame CreateGoto( GotoExpression gotoExpr, object value = null ) =>
        new(ControlFrameType.Goto, gotoExpr, value);

    public static ControlFrame CreateReturn( LabelTarget target, object value = null ) =>
        new(ControlFrameType.Return, target, value);

    public static ControlFrame CreateClosure( LambdaExpression lambdaExpr, Dictionary<ParameterExpression, object> scope ) =>
        new(ControlFrameType.Closure, lambdaExpr, scope);
}

internal enum ControlFrameType
{
    Goto,
    Return,
    Scan,
    Closure
}

public sealed class XsInterpreter : ExpressionVisitor
{
    private readonly InterpretScope _scope;
    private readonly XsDebugger _debugger;
    
    private readonly Stack<object> _resultStack = new();

    private Dictionary<GotoExpression, GotoNavigation> _gotoNavigation;
    private GotoNavigation _currentNavigation;
    private InterpreterMode _mode;

    private enum InterpreterMode
    {
        Evaluating,
        Navigating
    }

    public XsInterpreter( XsDebugger debugger = null )
    {
        _scope = new InterpretScope();
        _debugger = debugger;
    }

    public TDelegate Interpreter<TDelegate>( LambdaExpression expression )
        where TDelegate : Delegate
    {
        var invokeMethod = typeof(TDelegate).GetMethod( "Invoke" );
        
        if ( invokeMethod is null )
            throw new InvalidOperationException( "Invalid delegate type." );

        var returnType = invokeMethod.ReturnType;

        var evalMethod = typeof(XsInterpreter)
            .GetMethod( nameof(Evaluate), BindingFlags.NonPublic | BindingFlags.Instance )
            ?.MakeGenericMethod( returnType );
        
        if ( evalMethod is null )
            throw new InvalidOperationException( "Could not find InvokeEvaluatedExpression method." );

        var handlerDelegate = Delegate.CreateDelegate(
            typeof( Func<,,> ).MakeGenericType( typeof(LambdaExpression), typeof( object[] ), returnType ),
            this,
            evalMethod
        );

        var genericTypes = invokeMethod
            .GetParameters().Select( p => p.ParameterType )
            .Prepend( typeof(LambdaExpression) )
            .Append( returnType )
            .ToArray();

        var curryMethod = CurryFuncs.Methods
            .FirstOrDefault( m => m.Name == "Curry" && m.GetGenericArguments().Length == genericTypes.Length )
            ?.MakeGenericMethod( genericTypes );

        if ( curryMethod is null )
            throw new InvalidOperationException( $"No suitable Curry method found for delegate type {typeof(TDelegate)}" );

        PrepareNavigationMap( expression );

        return (TDelegate) curryMethod.Invoke( null, [handlerDelegate,expression] )!;

        void PrepareNavigationMap( Expression root )
        {
            if ( _gotoNavigation != null )
                return;

            var navigator = new GotoNavigationVisitor();
            _gotoNavigation = navigator.Analyze( root );
        }
    }

    private T Evaluate<T>( LambdaExpression lambda, params object[] values )
    {
        _scope.EnterScope( FrameType.Method );

        try
        {
            for ( var i = 0; i < lambda.Parameters.Count; i++ )
                _scope.Values[lambda.Parameters[i]] = values[i];

            Visit( lambda.Body );
            return (T) _resultStack.Pop();
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    //// Visit

    //public override Expression Visit( Expression node )
    //{
    //    if ( _mode != InterpreterMode.Navigating || _currentNavigation == null || _currentNavigation.CommonAncestor != node )
    //    {
    //        return base.Visit( node );
    //    }

    //    var nextStep = _currentNavigation.GetNextStep();
            
    //    if ( nextStep == null )
    //    {
    //        _currentNavigation = null;
    //        _mode = InterpreterMode.Evaluating;
    //        return node;
    //    }

    //    return Visit( nextStep );

    //}

    // Goto

    protected override Expression VisitGoto( GotoExpression node )
    {
        if ( !_gotoNavigation.TryGetValue( node, out var navigation ) )
            throw new InterpreterException( $"Undefined label target: {node.Target.Name}", node );

        _resultStack.Clear();

        if ( node.Kind == GotoExpressionKind.Return && node.Value != null )
        {
            Visit( node.Value );
        }

        _mode = InterpreterMode.Navigating;
        _currentNavigation = navigation;

        return node;
    }

    protected override Expression VisitLabel( LabelExpression node )
    {
        if ( _mode == InterpreterMode.Navigating && _currentNavigation!.TargetLabel == node.Target )
        {
            _mode = InterpreterMode.Evaluating;
            _currentNavigation.Reset();
            _currentNavigation = null;
        }

        return node;
    }

    // Block

    enum BlockState
    {
        InitializeVariables,
        HandleStatements,
        Complete
    };

    protected override Expression VisitBlock( BlockExpression node )
    {
        var state = BlockState.InitializeVariables;
        int statementIndex = 0;

        _scope.EnterScope( FrameType.Block );

        try
        {
            Navigate:

            if ( _mode == InterpreterMode.Navigating )
            {
                var nextStep = _currentNavigation.GetNextStep();
                statementIndex = node.Expressions.IndexOf( nextStep );
            }

            while ( true )
            {
                switch ( state )
                {
                    case BlockState.InitializeVariables:
                        foreach ( var variable in node.Variables )
                        {
                            _scope.Variables[variable.Name!] = variable;
                            _scope.Values[variable] = Default( variable.Type );
                        }

                        state = BlockState.HandleStatements;
                        break;

                    case BlockState.HandleStatements:
                        if ( statementIndex >= node.Expressions.Count )
                        {
                            state = BlockState.Complete;
                            break;
                        }

                        Visit( node.Expressions[statementIndex] );

                        if ( _mode == InterpreterMode.Navigating )
                        {
                            if ( _currentNavigation.CommonAncestor == node )
                                goto Navigate;

                            return node!;
                        }

                        statementIndex++;
                        break;

                    case BlockState.Complete:
                        return node;
                }
            }
        }
        finally
        {
            _scope.ExitScope();
        }

        static object Default( Type type ) =>
            type == typeof(string) ? string.Empty :
            type.IsValueType ? RuntimeHelpers.GetUninitializedObject( type ) : null;
    }

    // Conditional

    enum ConditionalState
    {
        Test, 
        HandleTest, 
        Visit, 
        Complete
    };

    protected override Expression VisitConditional( ConditionalExpression node )
    {
        var state = ConditionalState.Test;
        var continuation = ConditionalState.Complete;
        Expression expr = null;

    Navigate:

        if ( _mode == InterpreterMode.Navigating )
        {
            expr = _currentNavigation.GetNextStep();
            state = ConditionalState.Visit;
            continuation = expr == node.Test ? ConditionalState.HandleTest : ConditionalState.Complete;
        }

        while ( true )
        {
            switch ( state )
            {
                case ConditionalState.Test:
                    expr = node.Test;
                    state = ConditionalState.Visit;
                    continuation = ConditionalState.HandleTest;
                    break;

                case ConditionalState.HandleTest:
                    var conditionValue = (bool) _resultStack.Pop();
                    expr = conditionValue ? node.IfTrue : node.IfFalse;
                    state = ConditionalState.Visit;
                    continuation = ConditionalState.Complete;
                    break;

                case ConditionalState.Visit:
                    Visit( expr );

                    if ( _mode == InterpreterMode.Navigating )
                    {
                        if ( _currentNavigation.CommonAncestor == node )
                            goto Navigate;

                        return node; 
                    }

                    state = continuation;
                    break;

                case ConditionalState.Complete:
                    return node;
            }
        }
    }

    // Switch

    enum SwitchState
    {
        SwitchValue,
        HandleSwitchValue, 
        MatchCase,
        HandleMatchCase, 
        EvaluateCaseBody,
        Visit,
        VisitCaseBody,
        Complete 
    }

    protected override Expression VisitSwitch( SwitchExpression node )
    {
        var state = SwitchState.SwitchValue;
        var continuation = SwitchState.Complete;
        var caseIndex = 0;
        var testIndex = 0;
        object switchValue = null;
        Expression expr = null;

        Navigate:

        if ( _mode == InterpreterMode.Navigating )
        {
            expr = _currentNavigation.GetNextStep();

            if ( expr == node.SwitchValue )
            {
                state = SwitchState.Visit;
                continuation = SwitchState.HandleSwitchValue;
            }
            else
            {
                var matchedCase = node.Cases.FirstOrDefault( c => c.Body == expr );
                expr = matchedCase?.Body ?? node.DefaultBody;
                state = expr != null ? SwitchState.Visit : SwitchState.Complete;
                continuation = SwitchState.Complete;
            }
        }

        while ( true )
        {
            switch ( state )
            {
                case SwitchState.SwitchValue:
                    expr = node.SwitchValue;
                    state = SwitchState.Visit;
                    continuation = SwitchState.HandleSwitchValue;
                    break;

                case SwitchState.HandleSwitchValue:
                    switchValue = _resultStack.Pop();
                    caseIndex = 0;
                    testIndex = 0;
                    state = SwitchState.MatchCase;
                    break;

                case SwitchState.MatchCase:
                    if ( caseIndex >= node.Cases.Count )
                    {
                        expr = node.DefaultBody;
                        state = expr != null ? SwitchState.Visit : SwitchState.Complete;
                        continuation = SwitchState.Complete;
                        break;
                    }

                    var testValues = node.Cases[caseIndex].TestValues;
                    if ( testIndex >= testValues.Count )
                    {
                        caseIndex++;
                        testIndex = 0;
                        state = SwitchState.MatchCase;
                        break;
                    }

                    expr = testValues[testIndex];
                    state = SwitchState.Visit;
                    continuation = SwitchState.HandleMatchCase;
                    break;

                case SwitchState.HandleMatchCase:
                    var testValue = _resultStack.Pop();

                    if ( (switchValue != null && !switchValue.Equals( testValue )) || (switchValue == null && testValue != null) )
                    {
                        testIndex++;
                        state = SwitchState.MatchCase;
                        break;
                    }

                    expr = node.Cases[caseIndex].Body;
                    state = SwitchState.Visit;
                    continuation = SwitchState.Complete;
                    break;

                case SwitchState.Visit:
                    Visit( expr! );

                    if ( _mode == InterpreterMode.Navigating )
                    {
                        if ( _currentNavigation.CommonAncestor == node )
                            goto Navigate;

                        return node;
                    }

                    state = continuation;
                    break;

                case SwitchState.Complete:
                    return node;
            }
        }
    }

    // Loop

    // Lambda

    protected override Expression VisitLambda<T>( Expression<T> node )
    {
        if ( _scope.Depth == 0 )
        {
            _resultStack.Push( node ); 
            return node;
        }

        var freeVariables = FreeVariableVisitor.GetFreeVariables( node );

        if ( freeVariables.Count == 0 )
        {
            _resultStack.Push( node );
            return node;
        }

        var capturedScope = new Dictionary<ParameterExpression, object>();

        foreach ( var variable in freeVariables )
        {
            if ( !_scope.Values.TryGetValue( variable, out var value ) )
                throw new InterpreterException( $"Captured variable '{variable.Name}' is not defined.", node );

            capturedScope[variable] = value;
        }

        _resultStack.Push( ControlFrame.CreateClosure( node, capturedScope ) );
        return node;
    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        Visit( node.Expression );
        var targetValue = _resultStack.Pop();

        LambdaExpression lambda;
        Dictionary<ParameterExpression, object> capturedScope = null;

        switch ( targetValue )
        {
            case ControlFrame closure when closure.Type == ControlFrameType.Closure:
                lambda = closure.LambdaExpr;
                capturedScope = closure.State as Dictionary<ParameterExpression, object>;
                break;

            case LambdaExpression directLambda:
                lambda = directLambda;
                break;

            default:
                throw new InterpreterException( "Invocation target is not a valid lambda or closure.", node );
        }

        _scope.EnterScope( FrameType.Method );

        try
        {
            if ( capturedScope is not null )
            {
                foreach ( var (param, value) in capturedScope )
                    _scope.Values[param] = value;
            }

            for ( var i = 0; i < node.Arguments.Count; i++ )
            {
                Visit( node.Arguments[i] );
                _scope.Values[lambda.Parameters[i]] = _resultStack.Pop();
            }

            Visit( lambda.Body );
            return node;
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    protected override Expression VisitMethodCall( MethodCallExpression node )
    {
        var isStatic = node.Method.IsStatic;
        object instance = null;

        if ( !isStatic )
        {
            Visit( node.Object );
            instance = _resultStack.Pop();
        }

        var arguments = new object[node.Arguments.Count];
        var capturedValues = new Dictionary<int, Dictionary<ParameterExpression, object>>();
        var hasClosure = false;

        for ( var i = 0; i < node.Arguments.Count; i++ )
        {
            Visit( node.Arguments[i] );
            var argValue = _resultStack.Pop();

            switch ( argValue )
            {
                case ControlFrame closure when closure.Type == ControlFrameType.Closure:
                    hasClosure = true;
                    arguments[i] = this.Interpreter( closure.LambdaExpr );
                    capturedValues[i] = closure.State as Dictionary<ParameterExpression, object>;
                    break;

                case LambdaExpression lambdaExpr:
                    arguments[i] = this.Interpreter( lambdaExpr );
                    break;

                default:
                    arguments[i] = argValue;
                    break;
            }
        }

        if ( !hasClosure )
        {
            var result = node.Method.Invoke( instance, arguments );
            _resultStack.Push( result );
            return node;
        }

        _scope.EnterScope( FrameType.Method );

        try
        {
            foreach ( var (_, capturedScope) in capturedValues )
            {
                foreach ( var (param, value) in capturedScope )
                    _scope.Values[param] = value;
            }

            var result = node.Method.Invoke( instance, arguments );
            _resultStack.Push( result );
            return node;
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    protected override Expression VisitMember( MemberExpression node )
    {
        Visit( node.Expression );
        var instance = _resultStack.Pop();

        var result = node.Member switch
        {
            PropertyInfo prop => prop.GetValue( instance ),
            FieldInfo field => field.GetValue( instance ),
            _ => throw new InterpreterException( $"Unsupported member access: {node.Member.Name}", node )
        };

        _resultStack.Push( result );
        return node;
    }

    protected override Expression VisitBinary( BinaryExpression node )
    {
        Visit( node.Left );
        var left = _resultStack.Pop();

        Visit( node.Right );
        var right = _resultStack.Pop();

        var result = EvaluateBinary( node, left, right );
        _resultStack.Push( result );

        return node;
    }

    protected override Expression VisitTypeBinary( TypeBinaryExpression node )
    {
        Visit( node.Expression );
        var operand = _resultStack.Pop();

        var result = operand is not null && node.TypeOperand.IsAssignableFrom( operand.GetType() );

        _resultStack.Push( result );
        return node;
    }

    protected override Expression VisitUnary( UnaryExpression node )
    {
        Visit( node.Operand );
        var operand = _resultStack.Pop();

        var result = EvaluateUnary( node, operand );
        _resultStack.Push( result );

        return node;
    }

    protected override Expression VisitDefault( DefaultExpression node )
    {
        var defaultValue = node.Type.IsValueType && node.Type != typeof( void )
            ? RuntimeHelpers.GetUninitializedObject( node.Type )
            : null;

        _resultStack.Push( defaultValue );
        return node;
    }

    protected override Expression VisitConstant( ConstantExpression node )
    {
        _resultStack.Push( node.Value );
        return node;
    }

    protected override Expression VisitParameter( ParameterExpression node )
    {
        if ( !_scope.Values.TryGetValue( node, out var value ) )
            throw new InterpreterException( $"Parameter '{node.Name}' not found.", node );

        _resultStack.Push( value );
        return node;
    }

    protected override Expression VisitNew( NewExpression node )
    {
        var arguments = new object[node.Arguments.Count];
        var capturedValues = new Dictionary<int, Dictionary<ParameterExpression, object>>();
        var hasClosure = false;

        for ( var index = 0; index < node.Arguments.Count; index++ )
        {
            Visit( node.Arguments[index] );
            var argValue = _resultStack.Pop();

            switch ( argValue )
            {
                case ControlFrame closure when closure.Type == ControlFrameType.Closure:
                    hasClosure = true;
                    arguments[index] = this.Interpreter( closure.LambdaExpr );
                    capturedValues[index] = closure.State as Dictionary<ParameterExpression, object>;
                    break;

                case LambdaExpression lambdaExpr:
                    arguments[index] = this.Interpreter( lambdaExpr );
                    break;

                default:
                    arguments[index] = argValue;
                    break;
            }
        }

        var constructor = node.Constructor;

        if ( constructor is null )
        {
            throw new InterpreterException( $"No valid constructor found for type {node.Type}.", node );
        }

        if ( !hasClosure )
        {
            var instance = constructor.Invoke( arguments );
            _resultStack.Push( instance );
            return node;
        }

        _scope.EnterScope( FrameType.Method );

        try
        {
            foreach ( var (_, capturedScope) in capturedValues )
            {
                foreach ( var (param, value) in capturedScope )
                    _scope.Values[param] = value;
            }

            var instance = constructor.Invoke( arguments );
            _resultStack.Push( instance );
            return node;
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    protected override Expression VisitNewArray( NewArrayExpression node )
    {
        var elementType = node.Type.GetElementType();

        switch ( node.NodeType )
        {
            case ExpressionType.NewArrayInit:
            {
                // Handle NewArrayInit: Array initialized with values
                var values = new object[node.Expressions.Count];

                for ( var i = 0; i < node.Expressions.Count; i++ )
                {
                    Visit( node.Expressions[i] );
                    values[i] = _resultStack.Pop();
                }

                var array = Array.CreateInstance( elementType!, values.Length );

                for ( var i = 0; i < values.Length; i++ )
                    array.SetValue( values[i], i );

                _resultStack.Push( array );
                break;
            }
            case ExpressionType.NewArrayBounds:
            {
                // Handle NewArrayBounds: Array created with specified dimensions
                var lengths = new int[node.Expressions.Count];

                for ( var i = 0; i < node.Expressions.Count; i++ )
                {
                    Visit( node.Expressions[i] );
                    lengths[i] = (int) _resultStack.Pop();
                }

                var array = Array.CreateInstance( elementType!, lengths );
                _resultStack.Push( array );
                break;
            }
            default:
                throw new InterpreterException( $"Unsupported array creation type: {node.NodeType}", node );
        }

        return node;
    }

    protected override Expression VisitListInit( ListInitExpression node )
    {
        Visit( node.NewExpression );
        var instance = _resultStack.Pop();

        foreach ( var initializer in node.Initializers )
        {
            var arguments = new object[initializer.Arguments.Count];

            for ( var index = 0; index < initializer.Arguments.Count; index++ )
            {
                Visit( initializer.Arguments[index] );
                arguments[index] = _resultStack.Pop();
            }

            initializer.AddMethod.Invoke( instance, arguments );
        }

        _resultStack.Push( instance );
        return node;
    }

    private object EvaluateBinary( BinaryExpression binary, object left, object right )
    {
        switch ( binary.NodeType )
        {
            // Handle null-coalescing (??) operator
            case ExpressionType.Coalesce:
                return left ?? right;

            // Variable assignments (=, +=, -=, etc.)
            case ExpressionType.Assign:
            {
                if ( binary.Left is not ParameterExpression variable )
                    throw new InterpreterException( "Left side of assignment must be a variable.", binary );

                _scope.Values[variable] = right;
                return right;
            }
        }

        // Arithmetic Assignments (+=, -=, etc.)

        var leftType = left.GetType();

        if ( binary.NodeType is ExpressionType.AddAssign
            or ExpressionType.SubtractAssign
            or ExpressionType.MultiplyAssign
            or ExpressionType.DivideAssign
            or ExpressionType.ModuloAssign )
        {
            if ( binary.Left is not ParameterExpression variable )
                throw new InterpreterException( $"Left side of {binary.NodeType} must be a variable.", binary );

            object newValue = leftType switch
            {
                Type when leftType == typeof(int) => EvaluateArithmeticBinary( binary, (int) left!, (int) right! ),
                Type when leftType == typeof(long) => EvaluateArithmeticBinary( binary, (long) left!, (long) right! ),
                Type when leftType == typeof(float) => EvaluateArithmeticBinary( binary, (float) left!, (float) right! ),
                Type when leftType == typeof(double) => EvaluateArithmeticBinary( binary, (double) left!, (double) right! ),
                Type when leftType == typeof(decimal) => EvaluateArithmeticBinary( binary, (decimal) left!, (decimal) right! ),
                _ => throw new InterpreterException( $"Unsupported assignment operation: {binary.NodeType} for type {leftType}", binary ),
            };

            _scope.Values[variable] = newValue;
            return newValue;
        }

        // Logical Comparisons

        var rightType = right.GetType();
        var widenedType = GetWidenedType( leftType, rightType );

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

        // Regular Arithmetic Operations

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

    private static T EvaluateArithmeticBinary<T>( BinaryExpression binary, T left, T right )
        where T : INumber<T>
    {
        return binary.NodeType switch
        {
            ExpressionType.Add or ExpressionType.AddAssign => left + right,
            ExpressionType.Subtract or ExpressionType.SubtractAssign => left - right,
            ExpressionType.Multiply or ExpressionType.MultiplyAssign => left * right,
            ExpressionType.Divide or ExpressionType.DivideAssign => left / right,
            ExpressionType.Modulo or ExpressionType.ModuloAssign => left % right,
            ExpressionType.Power => T.CreateChecked( Math.Pow( double.CreateChecked( left ), double.CreateChecked( right ) ) ),
            _ => throw new InterpreterException( $"Unsupported arithmetic operation: {binary.NodeType}", binary )
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

    private object EvaluateUnary( UnaryExpression unary, object operand )
    {
        switch ( unary.NodeType )
        {
            case ExpressionType.Convert:
                if ( operand is not IConvertible convertible )
                    throw new InterpreterException( $"Cannot convert {operand.GetType()} to {unary.Type}", unary );

                return Convert.ChangeType( convertible, unary.Type );

            case ExpressionType.TypeAs:
                return operand is null || unary.Type.IsAssignableFrom( operand.GetType() ) ? operand : null;

            default:
                return operand switch
                {
                    int intValue => EvaluateNumericUnary( unary, intValue ),
                    long longValue => EvaluateNumericUnary( unary, longValue ),
                    float floatValue => EvaluateNumericUnary( unary, floatValue ),
                    double doubleValue => EvaluateNumericUnary( unary, doubleValue ),
                    decimal decimalValue => EvaluateNumericUnary( unary, decimalValue ),
                    bool boolValue => EvaluateLogicalUnary( unary, boolValue ),
                    _ => throw new InterpreterException( $"Unsupported unary operation for type {operand.GetType()}", unary )
                };
        }
    }

    private object EvaluateNumericUnary<T>( UnaryExpression unary, T operand )
        where T : INumber<T>
    {
        if ( unary.NodeType == ExpressionType.Negate )
            return -operand;

        if ( unary.Operand is not ParameterExpression variable )
            throw new InterpreterException( $"Unary assignment target must be a variable.", unary );

        T newValue;
        switch ( unary.NodeType )
        {

            case ExpressionType.PreIncrementAssign:
                newValue = operand + T.One;
                _scope.Values[variable] = newValue;
                return newValue; 

            case ExpressionType.PreDecrementAssign:
                newValue = operand - T.One;
                _scope.Values[variable] = newValue;
                return newValue; 

            case ExpressionType.PostIncrementAssign:
                newValue = operand + T.One;
                _scope.Values[variable] = newValue;
                return operand; 

            case ExpressionType.PostDecrementAssign:
                newValue = operand - T.One;
                _scope.Values[variable] = newValue;
                return operand; 

            default:
                throw new InterpreterException( $"Unsupported numeric unary operation: {unary.NodeType}", unary );
        }
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

    private sealed class FreeVariableVisitor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _declaredVariables = [];
        private readonly HashSet<ParameterExpression> _freeVariables = [];

        public static HashSet<ParameterExpression> GetFreeVariables( Expression expression )
        {
            var visitor = new FreeVariableVisitor();
            visitor.Visit( expression );
            return visitor._freeVariables;
        }

        protected override Expression VisitLambda<T>( Expression<T> node )
        {
            _declaredVariables.UnionWith( node.Parameters );

            return base.VisitLambda( node );
        }

        protected override Expression VisitParameter( ParameterExpression node )
        {
            if ( !_declaredVariables.Contains( node ) )
            {
                _freeVariables.Add( node );
            }

            return base.VisitParameter( node );
        }
    }
}

