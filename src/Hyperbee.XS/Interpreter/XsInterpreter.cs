using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
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

        return (TDelegate) curryMethod.Invoke( null, [handlerDelegate,expression] )!;
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

    private object PrepareLambdaInvocation( Closure closure )
    {
        _scope.EnterScope( FrameType.Method );
        try
        {
            foreach ( var kvp in closure.CapturedScope )
                _scope.Values[kvp.Key] = kvp.Value;

            return this.Interpreter( closure.Lambda );
        }
        finally
        {
            _scope.ExitScope();
        }
    }

    private object PrepareLambdaInvocation( LambdaExpression lambda )
    {
        return this.Interpreter( lambda );
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

        var result = EvaluateBinary( node, left, right );
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

        LabelTarget returnLabel = null;
        object returnValue = null;

        try
        {
            foreach ( var variable in node.Variables )
            {
                _scope.Variables[variable.Name!] = variable;
                _scope.Values[variable] = AssignDefault( variable.Type ); 
            }

            // BF original code
            //
            //foreach ( var expression in node.Expressions )
            //    Visit( expression );

            for ( var i = 0; i < node.Expressions.Count; i++ )
            {
                var expr = node.Expressions[i];

                // If this expression is a return, extract the return label
                if ( expr is GotoExpression gotoExpr && gotoExpr.Kind == GotoExpressionKind.Return )
                {
                    returnLabel = gotoExpr.Target;
                    Visit( gotoExpr.Value ); // Evaluate the return value
                    returnValue = _resultStack.Pop();
                    break; 
                }

                Visit( expr );

                // Pop intermediate results unless it's the last expression OR we hit a return
                if ( i < node.Expressions.Count - 1 )
                    _resultStack.TryPop( out _ );
            }
        }
        finally
        {
            _scope.ExitScope();
        }

        // If we encountered a return, push the return value onto the stack
        if ( returnLabel != null )
            _resultStack.Push( returnValue );

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

        _resultStack.Push( new Closure( node, capturedScope ) );
        return node;
    }

    protected override Expression VisitInvocation( InvocationExpression node )
    {
        Visit( node.Expression );

        var target = _resultStack.Pop();

        if ( target is Closure closure )
        {
            _scope.EnterScope( FrameType.Method );

            try
            {
                foreach ( var (param, value) in closure.CapturedScope )
                    _scope.Values[param] = value;

                for ( var i = 0; i < node.Arguments.Count; i++ )
                {
                    Visit( node.Arguments[i] );
                    _scope.Values[closure.Lambda.Parameters[i]] = _resultStack.Pop();
                }

                Visit( closure.Lambda.Body );
                return node;
            }
            finally
            {
                _scope.ExitScope();
            }
        }

        if ( target is not LambdaExpression lambda )
        {
            throw new InterpreterException( "Invocation target is not a valid lambda or closure.", node );
        }

        // Direct invocation of a raw lambda (outermost lambda case)

        _scope.EnterScope( FrameType.Method );

        try
        {
            for ( var i = 0; i < lambda.Parameters.Count; i++ )
            {
                Visit( node.Arguments[i] );
                _scope.Values[lambda.Parameters[i]] = _resultStack.Pop();
            }

            Visit( lambda.Body );
        }
        finally
        {
            _scope.ExitScope();
        }

        return node;
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

        var argumentCount = node.Arguments.Count;
        var arguments = new object[argumentCount];

        for ( var i = 0; i < argumentCount; i++ )
        {
            Visit( node.Arguments[i] );
            var argument = _resultStack.Pop();

            arguments[i] = argument switch
            {
                Closure closure => PrepareLambdaInvocation( closure ),
                LambdaExpression lambda => PrepareLambdaInvocation( lambda ),
                _ => argument
            };
        }

        var result = node.Method.Invoke( instance, arguments );
        _resultStack.Push( result );
        return node;
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

    protected override Expression VisitNew( NewExpression node )
    {
        var arguments = new object[node.Arguments.Count];

        for ( var i = 0; i < node.Arguments.Count; i++ )
        {
            Visit( node.Arguments[i] );
            var argument = _resultStack.Pop();

            arguments[i] = argument switch
            {
                Closure closure => PrepareLambdaInvocation( closure ),
                LambdaExpression lambda => PrepareLambdaInvocation( lambda ),
                _ => argument
            };
        }

        var constructor = node.Constructor
            ?? throw new InterpreterException( $"No valid constructor found for type {node.Type}.", node );

        var instance = constructor.Invoke( arguments );

        _resultStack.Push( instance );

        return node;
    }

    protected override Expression VisitNewArray( NewArrayExpression node )
    {
        var elementType = node.Type.GetElementType();
        var elements = new object[node.Expressions.Count];

        for ( var i = 0; i < node.Expressions.Count; i++ )
        {
            Visit( node.Expressions[i] );
            elements[i] = _resultStack.Pop();
        }

        var array = Array.CreateInstance( elementType!, elements.Length );

        for ( var i = 0; i < elements.Length; i++ )
            array.SetValue( elements[i], i );

        _resultStack.Push( array );
        return node;
    }

    private object EvaluateBinary( BinaryExpression binary, object left, object right )
    {
        // Variable assignments (=, +=, -=, etc.)

        if ( binary.NodeType == ExpressionType.Assign )
        {
            if ( binary.Left is not ParameterExpression variable )
                throw new InterpreterException( "Left side of assignment must be a variable.", binary );

            _scope.Values[variable] = right;
            return right;
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
        if ( unary.NodeType == ExpressionType.Convert )
        {
            if ( operand is not IConvertible convertible )
                throw new InterpreterException( $"Cannot convert {operand.GetType()} to {unary.Type}", unary );

            return Convert.ChangeType( convertible, unary.Type );
        }

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

    private sealed class Closure
    {
        public LambdaExpression Lambda { get; }
        public Dictionary<ParameterExpression, object> CapturedScope { get; }

        public Closure( LambdaExpression lambda, Dictionary<ParameterExpression, object> capturedScope )
        {
            Lambda = lambda;
            CapturedScope = capturedScope;
        }
    }

    public static class CurryFuncs
    {
        public static readonly MethodInfo[] Methods = typeof(CurryFuncs).GetMethods();

        public static Func<R> Curry<C, R>( Func<C, object[], R> f, C c ) =>
            () => f( c, [] );

        public static Func<T1, R> Curry<C, T1, R>( Func<C, object[], R> f, C c ) =>
            t1 => f( c, [t1] );

        public static Func<T1, T2, R> Curry<C, T1, T2, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2 ) => f( c, [t1, t2] );

        public static Func<T1, T2, T3, R> Curry<C, T1, T2, T3, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2, t3 ) => f( c, [t1, t2, t3] );

        public static Func<T1, T2, T3, T4, R> Curry<C, T1, T2, T3, T4, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2, t3, t4 ) => f( c, [t1, t2, t3, t4] );

        public static Func<T1, T2, T3, T4, T5, R> Curry<C, T1, T2, T3, T4, T5, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2, t3, t4, t5 ) => f( c, [t1, t2, t3, t4, t5] );

        public static Func<T1, T2, T3, T4, T5, T6, R> Curry<C, T1, T2, T3, T4, T5, T6, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2, t3, t4, t5, t6 ) => f( c, [t1, t2, t3, t4, t5, t6] );

        public static Func<T1, T2, T3, T4, T5, T6, T7, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, R>( Func<C, object[], R> f, C c ) =>
            ( t1, t2, t3, t4, t5, t6, t7 ) => f( c, [t1, t2, t3, t4, t5, t6, t7] );
    }
}
