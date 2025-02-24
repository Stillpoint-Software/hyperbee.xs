using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.Interpreter;

internal sealed class Evaluator
{
    private readonly UnaryEvaluator _unary;
    private readonly BinaryEvaluator _binary;

    public Evaluator( XsInterpreter interpreter )
    {
        _unary = new( interpreter );
        _binary = new( interpreter );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public object Unary( UnaryExpression unary, object operand )
    {
        return _unary.Unary( unary, operand );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public object Binary( BinaryExpression binary, object leftValue, object rightValue )
    {
        return _binary.Binary( binary, leftValue, rightValue );
    }
}
