using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Interpreter;

public static class LambdaExpressionExtensions
{
    public static object Interpreter( this LambdaExpression lambda )
    {
        return Interpreter( new XsInterpreter(), lambda );
    }

    internal static object Interpreter( this XsInterpreter interpreter, LambdaExpression lambda )
    {
        if ( !typeof(Delegate).IsAssignableFrom( lambda.Type ) )
            throw new InvalidOperationException( "LambdaExpression must be convertible to a delegate." );

        var invokeMethod = lambda.Type.GetMethod( "Invoke" );
        
        if ( invokeMethod is null )
            throw new InvalidOperationException( "Invalid delegate type." );

        var paramTypes = invokeMethod.GetParameters()
            .Select( p => p.ParameterType )
            .Append( invokeMethod.ReturnType )
            .ToArray();

        var delegateType = Expression.GetDelegateType( paramTypes );

        var method = typeof(XsInterpreter)
            .GetMethod( nameof(XsInterpreter.Interpreter), BindingFlags.Public | BindingFlags.Instance )?
            .MakeGenericMethod( delegateType );

        var interpretedDelegate = method?.Invoke( interpreter, [lambda] );

        if ( interpretedDelegate == null )
            throw new InvalidOperationException( "Failed to create interpreted delegate." );

        return interpretedDelegate; 
    }

    public static TDelegate Interpreter<TDelegate>( this Expression<TDelegate> lambda )
        where TDelegate : Delegate
    {
        return (TDelegate) Interpreter( (LambdaExpression) lambda );
    }
}


