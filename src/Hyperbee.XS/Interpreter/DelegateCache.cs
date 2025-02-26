using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Interpreter;

internal static class DelegateCache
{
    private static readonly Dictionary<Type, MethodInfo> EvalMethods = new();
    private static readonly Dictionary<(Type, Type[]), MethodInfo> DelegateBinders = new();

    static DelegateCache()
    {
        EvalMethods[typeof(void)] = typeof(XsInterpreter).GetMethod( nameof(XsInterpreter.EvaluateVoid), BindingFlags.NonPublic | BindingFlags.Instance );
    }

    public static void Get<TDelegate>( LambdaExpression expression, XsInterpreter interpreter, out MethodInfo delegateBinder, out Delegate delegateHandler )
        where TDelegate : Delegate
    {
        var invokeMethod = typeof(TDelegate).GetMethod( "Invoke" );

        if ( invokeMethod is null )
            throw new InvalidOperationException( "Invalid delegate type." );

        var returnType = invokeMethod.ReturnType;

        if ( returnType == typeof(void) )
        {
            delegateHandler = Delegate.CreateDelegate(
                typeof(Action<,>).MakeGenericType( typeof(LambdaExpression), typeof(object[]) ),
                interpreter,
                GetEvalMethod( typeof(void) )
            );
        }
        else
        {
            delegateHandler = Delegate.CreateDelegate(
                typeof(Func<,,>).MakeGenericType( typeof(LambdaExpression), typeof(object[]), returnType ),
                interpreter,
                GetEvalMethod( returnType )
            );
        }

        var genericTypes = invokeMethod
            .GetParameters().Select( p => p.ParameterType )
            .Prepend( typeof(LambdaExpression) )
            .Concat( returnType == typeof(void) ? Array.Empty<Type>() : new[] { returnType } )
            .ToArray();

        delegateBinder = GetDelegateBinder( genericTypes, returnType == typeof(void) );

        if ( delegateBinder is null )
            throw new InvalidOperationException( $"No suitable bind method found for delegate type {typeof(TDelegate)}" );

    }

    private static MethodInfo GetEvalMethod( Type returnType )
    {
        if ( EvalMethods.TryGetValue( returnType, out var method ) )
            return method;

        method = typeof(XsInterpreter)
            .GetMethod( nameof(XsInterpreter.Evaluate), BindingFlags.NonPublic | BindingFlags.Instance )
            ?.MakeGenericMethod( returnType );

        EvalMethods[returnType] = method;

        return method;
    }

    private static MethodInfo GetDelegateBinder( Type[] genericTypes, bool isVoid )
    {
        var key = (isVoid ? typeof(void) : typeof(object), genericTypes);

        if ( DelegateBinders.TryGetValue( key, out var method ) )
            return method;

        var methodSource = isVoid
            ? ActionBinder.Methods
            : FuncBinder.Methods;

        method = methodSource
            .FirstOrDefault( m => m.GetGenericArguments().Length == genericTypes.Length )
            ?.MakeGenericMethod( genericTypes );

        DelegateBinders[key] = method;

        return method;
    }
}
