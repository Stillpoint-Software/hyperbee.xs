using System.Reflection;

namespace Hyperbee.XS.Interpreter;

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

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8] );

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9] );

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10] );
}
