using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Interpreter;

public static class LambdaExpressionExtensions
{
    public static object Interpreter( this LambdaExpression lambda )
    {
        if ( !typeof(Delegate).IsAssignableFrom( lambda.Type ) )
            throw new InvalidOperationException( "LambdaExpression must be convertible to a delegate." );

        var invokeMethod = lambda.Type.GetMethod( "Invoke" )
                           ?? throw new InvalidOperationException( "Invalid delegate type." );
        
        var returnType = invokeMethod.ReturnType;

        var method = typeof(XsInterpreter)
            .GetMethod( nameof(XsInterpreter.Interpreter), BindingFlags.Public | BindingFlags.Instance )?
            .MakeGenericMethod( returnType );

        var xsInterpreter = new XsInterpreter();

        var interpretedDelegate = method?.Invoke( xsInterpreter, [lambda] )
                                  ?? throw new InvalidOperationException( "Failed to create interpreted delegate." );

        return interpretedDelegate;
    }

    public static TDelegate Interpreter<TDelegate>( this Expression<TDelegate> lambda )
        where TDelegate : Delegate
    {
        return (TDelegate) Interpreter((LambdaExpression) lambda); // Call the non-generic version and cast the result
    }
}
