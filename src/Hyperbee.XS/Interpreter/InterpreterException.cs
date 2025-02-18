using System.Linq.Expressions;

namespace Hyperbee.XS.Interpreter;

public class InterpreterException : Exception
{
    public Expression Expression { get; }

    public InterpreterException( string message, Expression expression )
        : base( FormatMessage( message, expression ) )
    {
        Expression = expression;
    }

    private static string FormatMessage( string message, Expression expression )
    {
        return $"Interpreter Error: {message}\nExpression: {expression}";
    }
}
