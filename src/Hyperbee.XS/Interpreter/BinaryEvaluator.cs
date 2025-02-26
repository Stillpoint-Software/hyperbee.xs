using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Hyperbee.XS.Core;

namespace Hyperbee.XS.Interpreter;

internal sealed class BinaryEvaluator
{
    private readonly XsInterpreter _interpreter;

    public BinaryEvaluator( XsInterpreter interpreter )
    {
        _interpreter = interpreter;
    }

    public object Binary( BinaryExpression binary )
    {
        var operation = GetOperation( binary );

        switch ( binary.NodeType )
        {
            case ExpressionType.Coalesce:
                return CoalesceOperation( operation );

            case ExpressionType.Assign:
            case ExpressionType.AddAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShiftAssign:
                return AssignOperation( operation );

            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThanOrEqual:
                return LogicalOperation( operation );

            default:
                return ArithmeticOperation( operation );
        }
    }

    private BinaryOperation GetOperation( BinaryExpression binary )
    {
        object leftValue;
        object leftInstance = null;
        object[] index = null;

        object rightValue = _interpreter.ResultStack.Pop();

        switch ( binary.Left )
        {
            case ParameterExpression paramExpr:
                leftValue = _interpreter.Scope.Values[paramExpr];
                break;

            case MemberExpression memberExpr:
                leftInstance = _interpreter.ResultStack.Pop();
                leftValue = GetMemberValue( leftInstance, memberExpr );
                break;

            case IndexExpression indexExpr:
                index = new object[indexExpr.Arguments.Count];
                for ( var i = indexExpr.Arguments.Count - 1; i >= 0; i-- )
                {
                    index[i] = _interpreter.ResultStack.Pop();
                }

                leftInstance = _interpreter.ResultStack.Pop();
                leftValue = GetIndexValue( leftInstance, indexExpr, index );
                break;

            default:
                leftValue = _interpreter.ResultStack.Pop();
                break;
        }

        return new BinaryOperation( binary, leftValue, rightValue, leftInstance, index );

        // Helper methods

        static object GetMemberValue( object instance, MemberExpression memberExpr )
        {
            return memberExpr.Member switch
            {
                PropertyInfo prop => prop.GetValue( instance )
                    ?? throw new InterpreterException( $"Property {prop.Name} evaluated to null.", memberExpr ),

                FieldInfo field => field.GetValue( instance )
                    ?? throw new InterpreterException( $"Field {field.Name} evaluated to null.", memberExpr ),

                _ => throw new InterpreterException( $"Unsupported member access: {memberExpr.Member.Name}", memberExpr )
            };
        }

        static object GetIndexValue( object instance, IndexExpression indexExpr, object[] index )
        {
            return indexExpr.Indexer!.GetValue( instance, index )
                   ?? throw new InterpreterException( $"Index access on {indexExpr.Indexer.Name} evaluated to null.", indexExpr );
        }
    }

    private object AssignOperation( BinaryOperation operation )
    {
        var (binary, rightValue, leftInstance, indexArguments) = operation;

        if ( binary.NodeType != ExpressionType.Assign )
            rightValue = ArithmeticOperation( operation );

        switch ( binary.Left )
        {
            case ParameterExpression paramExpr:
                return _interpreter.Scope.Values[Collections.LinkedNode.Single, paramExpr] = rightValue;

            case MemberExpression memberExpr:
                return AssignToMember( memberExpr, leftInstance, rightValue );

            case IndexExpression indexExpr:
                return AssignToIndex( indexExpr, leftInstance, indexArguments, rightValue );

            default:
                throw new InterpreterException( $"Unsupported left-hand side in assignment: {binary.Left.GetType().Name}", binary );
        }

        // Helper methods

        static object AssignToIndex( IndexExpression indexExpr, object leftInstance, object[] index, object rightValue )
        {
            indexExpr.Indexer!.SetValue( leftInstance, rightValue, index );
            return rightValue;
        }

        static object AssignToMember( MemberExpression memberExpr, object leftInstance, object rightValue )
        {
            switch ( memberExpr.Member )
            {
                case PropertyInfo prop when prop.CanWrite:
                    prop.SetValue( leftInstance, rightValue );
                    return rightValue;

                case FieldInfo field:
                    field.SetValue( leftInstance, rightValue );
                    return rightValue;

                default:
                    throw new InterpreterException( $"Cannot assign to member: {memberExpr.Member.Name}", memberExpr );
            }
        }
    }

    private static object CoalesceOperation( BinaryOperation operation )
    {
        var (leftValue, rightValue) = operation;
        return leftValue ?? rightValue;
    }

    private static object ArithmeticOperation( BinaryOperation operation )
    {
        var (binary, leftValue, rightValue) = operation;

        if ( binary.NodeType == ExpressionType.LeftShiftAssign || binary.NodeType == ExpressionType.RightShiftAssign )
            return ShiftOperation( operation );

        var widenedType = GetWidenedType( leftValue.GetType(), rightValue.GetType() );

        return widenedType switch
        {
            Type when widenedType == typeof( int ) => ArithmeticOperation( binary, (int) leftValue!, (int) rightValue! ),
            Type when widenedType == typeof( long ) => ArithmeticOperation( binary, (long) leftValue!, (long) rightValue! ),
            Type when widenedType == typeof( short ) => ArithmeticOperation( binary, (short) leftValue!, (short) rightValue! ),
            Type when widenedType == typeof( float ) => ArithmeticOperation( binary, (float) leftValue!, (float) rightValue! ),
            Type when widenedType == typeof( double ) => ArithmeticOperation( binary, (double) leftValue!, (double) rightValue! ),
            Type when widenedType == typeof( decimal ) => ArithmeticOperation( binary, (decimal) leftValue!, (decimal) rightValue! ),
            _ => throw new InterpreterException( $"Unsupported binary operation: {binary.NodeType}", binary )
        };
    }

    private static T ArithmeticOperation<T>( BinaryExpression binary, T leftValue, T rightValue )
        where T : INumber<T>
    {
        return binary.NodeType switch
        {
            ExpressionType.Add or ExpressionType.AddAssign => leftValue + rightValue,
            ExpressionType.Subtract or ExpressionType.SubtractAssign => leftValue - rightValue,
            ExpressionType.Multiply or ExpressionType.MultiplyAssign => leftValue * rightValue,
            ExpressionType.Divide or ExpressionType.DivideAssign => leftValue / rightValue,
            ExpressionType.Modulo or ExpressionType.ModuloAssign => leftValue % rightValue,
            ExpressionType.Power => T.CreateChecked( Math.Pow( double.CreateChecked( leftValue ), double.CreateChecked( rightValue ) ) ),
            _ => throw new InterpreterException( $"Unsupported arithmetic operation: {binary.NodeType}", binary )
        };
    }

    private static bool LogicalOperation( BinaryOperation operation )
    {
        var (binary, leftValue, rightValue) = operation;

        var widenedType = GetWidenedType( leftValue.GetType(), rightValue.GetType() );

        return widenedType switch
        {
            Type when widenedType == typeof( int ) => LogicalOperation( binary, (int) leftValue!, (int) rightValue! ),
            Type when widenedType == typeof( long ) => LogicalOperation( binary, (long) leftValue!, (long) rightValue! ),
            Type when widenedType == typeof(short) => LogicalOperation( binary, (short) leftValue!, (short) rightValue! ),
            Type when widenedType == typeof( float ) => LogicalOperation( binary, (float) leftValue!, (float) rightValue! ),
            Type when widenedType == typeof( double ) => LogicalOperation( binary, (double) leftValue!, (double) rightValue! ),
            Type when widenedType == typeof( decimal ) => LogicalOperation( binary, (decimal) leftValue!, (decimal) rightValue! ),
            _ => throw new InterpreterException( $"Unsupported logical operation: {binary.NodeType}", binary )
        };
    }

    private static bool LogicalOperation<T>( BinaryExpression binary, T leftValue, T rightValue )
        where T : IComparable<T>
    {
        return binary.NodeType switch
        {
            ExpressionType.LessThan => leftValue.CompareTo( rightValue ) < 0,
            ExpressionType.GreaterThan => leftValue.CompareTo( rightValue ) > 0,
            ExpressionType.LessThanOrEqual => leftValue.CompareTo( rightValue ) <= 0,
            ExpressionType.GreaterThanOrEqual => leftValue.CompareTo( rightValue ) >= 0,
            ExpressionType.Equal => leftValue.Equals( rightValue ),
            ExpressionType.NotEqual => !leftValue.Equals( rightValue ),
            _ => throw new InterpreterException( $"Unsupported logical operation: {binary.NodeType}", binary )
        };
    }

    private static object ShiftOperation( BinaryOperation operation )
    {
        var (binary, leftValue, rightValue) = operation;

        if ( rightValue is not int shiftAmount )
            throw new InterpreterException( $"Shift amount must be an integer: {rightValue.GetType()}", binary );

        var shiftLeft = binary.NodeType == ExpressionType.LeftShiftAssign;

        return leftValue switch
        {
            int intValue => shiftLeft ? intValue << shiftAmount : intValue >> shiftAmount,
            long longValue => shiftLeft ? longValue << shiftAmount : longValue >> shiftAmount,
            uint uintValue => shiftLeft ? uintValue << shiftAmount : uintValue >> shiftAmount,
            ulong ulongValue => shiftLeft ? ulongValue << shiftAmount : ulongValue >> shiftAmount,
            short shortValue => shiftLeft ? (short) (shortValue << shiftAmount) : (short) (shortValue >> shiftAmount),
            ushort ushortValue => shiftLeft ? (ushort) (ushortValue << shiftAmount) : (ushort) (ushortValue >> shiftAmount),
            byte byteValue => shiftLeft ? (byte) (byteValue << shiftAmount) : (byte) (byteValue >> shiftAmount),
            sbyte sbyteValue => shiftLeft ? (sbyte) (sbyteValue << shiftAmount) : (sbyte) (sbyteValue >> shiftAmount),
            _ => throw new InterpreterException( $"Unsupported type for shift assignment: {leftValue.GetType()}", binary )
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

    private readonly ref struct BinaryOperation
    {
        public BinaryExpression Binary { get; }
        public object LeftValue { get; }
        public object RightValue { get; }
        public object LeftInstance { get; }
        public object[] IndexArguments { get; }

        public BinaryOperation( BinaryExpression binary, object leftValue, object rightValue, object leftInstance = default, object[] indexArguments = default )
        {
            Binary = binary;
            LeftValue = leftValue;
            RightValue = rightValue;
            LeftInstance = leftInstance;
            IndexArguments = indexArguments;
        }

        public void Deconstruct( out object leftValue, out object rightValue )
        {
            leftValue = LeftValue;
            rightValue = RightValue;
        }

        public void Deconstruct( out BinaryExpression binary, out object leftValue, out object rightValue )
        {
            binary = Binary;
            leftValue = LeftValue;
            rightValue = RightValue;
        }

        public void Deconstruct( out BinaryExpression binary, out object rightValue, out object leftInstance, out object[] indexArguments )
        {
            binary = Binary;
            rightValue = RightValue;
            leftInstance = LeftInstance;
            indexArguments = IndexArguments;
        }
    }
}
