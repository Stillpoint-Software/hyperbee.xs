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

    public object Binary( BinaryExpression binary, object leftValue, object rightValue )
    {
        switch ( binary.NodeType )
        {
            case ExpressionType.Coalesce:
                return leftValue ?? rightValue;

            case ExpressionType.Assign:
                return Assign( binary, rightValue );

            case ExpressionType.AddAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.LeftShiftAssign: 
            case ExpressionType.RightShiftAssign: 
                return Assign( binary, leftValue, rightValue );

            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThanOrEqual:
                return LogicalBinary( binary, leftValue, rightValue );

            default:
                return ArithmeticBinary( binary, leftValue, rightValue );
        }
    }

    private object Assign( BinaryExpression binary, object value )
    {
        switch ( binary.Left )
        {
            case ParameterExpression variable:
                _interpreter.Scope.Values[variable] = value;
                return value;

            case MemberExpression memberExpr:
                return AssignToMember( memberExpr, value );

            case IndexExpression indexExpr:
                return AssignToIndex( indexExpr, value );

            default:
                throw new InterpreterException( $"Unsupported left-hand side in assignment: {binary.Left.GetType().Name}", binary );
        }
    }

    private object Assign( BinaryExpression binary, object leftValue, object rightValue )
    {
        object newValue;

        switch ( binary.NodeType )
        {
            case ExpressionType.AddAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ModuloAssign:
                newValue = ArithmeticBinary( binary, leftValue, rightValue );
                break;

            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShiftAssign:
                newValue = ShiftBinary( binary, leftValue, rightValue );
                break;

            default:
                throw new InterpreterException( $"Unsupported assignment operation: {binary.NodeType}", binary );
        }

        switch ( binary.Left )
        {
            case ParameterExpression variable:
                _interpreter.Scope.Values[variable] = newValue;
                return newValue;

            case MemberExpression memberExpr:
                return AssignToMember( memberExpr, newValue );

            case IndexExpression indexExpr:
                return AssignToIndex( indexExpr, newValue );

            default:
                throw new InterpreterException( $"Unsupported left-hand side in {binary.NodeType}: {binary.Left.GetType().Name}", binary );
        }
    }

    private object AssignToMember( MemberExpression memberExpr, object value )
    {
        _interpreter.Visit( memberExpr.Expression );
        var instance = _interpreter.ResultStack.Pop();

        switch ( memberExpr.Member )
        {
            case PropertyInfo prop when prop.CanWrite:
                prop.SetValue( instance, value );
                return value;

            case FieldInfo field:
                field.SetValue( instance, value );
                return value;

            default:
                throw new InterpreterException( $"Cannot assign to member: {memberExpr.Member.Name}", memberExpr );
        }
    }

    private object AssignToIndex( IndexExpression indexExpr, object value )
    {
        throw new NotImplementedException(); // BF ME discuss

        //var arguments = new object[indexExpr.Arguments.Count];

        //for ( var i = indexExpr.Arguments.Count - 1; i >= 0; i-- )
        //{
        //    arguments[i] = _resultStack.Pop(); // Pop evaluated arguments
        //}

        //var instance = _resultStack.Pop(); // Pop evaluated instance

        //indexExpr.Indexer.SetValue( instance, value, arguments );
        //return value;
    }


    private object LogicalBinary( BinaryExpression binary, object leftValue, object rightValue )
    {
        var widenedType = GetWidenedType( leftValue.GetType(), rightValue.GetType() );

        return widenedType switch
        {
            Type when widenedType == typeof(int) => EvaluateLogicalBinary( binary, (int) leftValue!, (int) rightValue! ),
            Type when widenedType == typeof(long) => EvaluateLogicalBinary( binary, (long) leftValue!, (long) rightValue! ),
            Type when widenedType == typeof(float) => EvaluateLogicalBinary( binary, (float) leftValue!, (float) rightValue! ),
            Type when widenedType == typeof(double) => EvaluateLogicalBinary( binary, (double) leftValue!, (double) rightValue! ),
            Type when widenedType == typeof(decimal) => EvaluateLogicalBinary( binary, (decimal) leftValue!, (decimal) rightValue! ),
            _ => throw new InterpreterException( $"Unsupported logical operation: {binary.NodeType}", binary )
        };
    }

    private object ArithmeticBinary( BinaryExpression binary, object leftValue, object rightValue )
    {
        var widenedType = GetWidenedType( leftValue.GetType(), rightValue.GetType() );

        return widenedType switch
        {
            Type when widenedType == typeof(int) => EvaluateArithmeticBinary( binary, (int) leftValue!, (int) rightValue! ),
            Type when widenedType == typeof(long) => EvaluateArithmeticBinary( binary, (long) leftValue!, (long) rightValue! ),
            Type when widenedType == typeof(float) => EvaluateArithmeticBinary( binary, (float) leftValue!, (float) rightValue! ),
            Type when widenedType == typeof(double) => EvaluateArithmeticBinary( binary, (double) leftValue!, (double) rightValue! ),
            Type when widenedType == typeof(decimal) => EvaluateArithmeticBinary( binary, (decimal) leftValue!, (decimal) rightValue! ),
            _ => throw new InterpreterException( $"Unsupported binary operation: {binary.NodeType}", binary )
        };
    }

    private static object ShiftBinary( BinaryExpression binary, object leftValue, object rightValue )
    {
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


    private static T EvaluateArithmeticBinary<T>( BinaryExpression binary, T leftValue, T rightValue )
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

    private static bool EvaluateLogicalBinary<T>( BinaryExpression binary, T leftValue, T rightValue )
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
