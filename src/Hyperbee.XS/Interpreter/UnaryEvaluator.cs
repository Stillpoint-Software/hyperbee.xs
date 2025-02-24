using System.Linq.Expressions;
using System.Numerics;

namespace Hyperbee.XS.Interpreter;

internal sealed class UnaryEvaluator
{
    private readonly XsInterpreter _interpreter;

    public UnaryEvaluator( XsInterpreter interpreter )
    {
        _interpreter = interpreter;
    }

    public object Unary( UnaryExpression unary, object operand )
    {
        switch ( unary.NodeType )
        {
            case ExpressionType.Convert:
                return ConvertOperand( unary, operand );

            case ExpressionType.TypeAs:
                return operand is null || unary.Type.IsAssignableFrom( operand.GetType() ) ? operand : null;

            case ExpressionType.Not:
            case ExpressionType.IsFalse:
            case ExpressionType.IsTrue:
                return LogicalUnary( unary, (bool) operand );

            case ExpressionType.Negate:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
                return NumericUnary( unary, operand );

            case ExpressionType.OnesComplement:
                return OnesComplement( unary, operand );

            default:
                throw new InterpreterException( $"Unsupported unary operation: {unary.NodeType}", unary );
        }
    }

    private static object ConvertOperand( UnaryExpression unary, object operand )
    {
        if ( operand is not IConvertible convertible )
            throw new InterpreterException( $"Cannot convert {operand.GetType()} to {unary.Type}", unary );

        return Convert.ChangeType( convertible, unary.Type );
    }

    private static bool LogicalUnary( UnaryExpression unary, bool operand )
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => !operand,
            ExpressionType.IsFalse => !operand,
            ExpressionType.IsTrue => operand,
            _ => throw new InterpreterException( $"Unsupported boolean unary operation: {unary.NodeType}", unary )
        };
    }

    private object NumericUnary( UnaryExpression unary, object operand )
    {
        return operand switch
        {
            int intValue => EvaluateNumericUnary( unary, intValue ),
            long longValue => EvaluateNumericUnary( unary, longValue ),
            float floatValue => EvaluateNumericUnary( unary, floatValue ),
            double doubleValue => EvaluateNumericUnary( unary, doubleValue ),
            decimal decimalValue => EvaluateNumericUnary( unary, decimalValue ),
            _ => throw new InterpreterException( $"Unsupported unary operation for type {operand.GetType()}", unary )
        };
    }

    private object EvaluateNumericUnary<T>( UnaryExpression unary, T operand )
        where T : INumber<T>
    {
        if ( unary.NodeType == ExpressionType.Negate )
            return -operand;

        if ( unary.Operand is not ParameterExpression variable )
            throw new InterpreterException( "Unary target must be a variable.", unary );

        T newValue;
        switch ( unary.NodeType )
        {
            case ExpressionType.PreIncrementAssign:
                newValue = operand + T.One;
                _interpreter.Scope.Values[variable] = newValue;
                return newValue;

            case ExpressionType.PreDecrementAssign:
                newValue = operand - T.One;
                _interpreter.Scope.Values[variable] = newValue;
                return newValue;

            case ExpressionType.PostIncrementAssign:
                newValue = operand + T.One;
                _interpreter.Scope.Values[variable] = newValue;
                return operand;

            case ExpressionType.PostDecrementAssign:
                newValue = operand - T.One;
                _interpreter.Scope.Values[variable] = newValue;
                return operand;

            default:
                throw new InterpreterException( $"Unsupported numeric unary operation: {unary.NodeType}", unary );
        }
    }

    private static object OnesComplement( UnaryExpression unary, object operand )
    {
        return operand switch
        {
            int intValue => ~intValue,
            long longValue => ~longValue,
            short shortValue => ~shortValue,
            byte byteValue => (byte) ~byteValue,
            sbyte sbyteValue => (sbyte) ~sbyteValue,
            uint uintValue => ~uintValue,
            ulong ulongValue => ~ulongValue,
            ushort ushortValue => (ushort) ~ushortValue,
            _ => throw new InterpreterException( $"Unsupported type for OnesComplement: {operand.GetType()}", unary )
        };
    }
}
