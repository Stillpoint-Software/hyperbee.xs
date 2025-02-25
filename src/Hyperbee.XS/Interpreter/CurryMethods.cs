using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.Interpreter;

internal static class CurryFunc
{
    public static readonly MethodInfo[] Methods = typeof( CurryFunc ).GetMethods();

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<R> Curry<C, R>( Func<C, object[], R> f, C c ) =>
        () => f( c, [] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, R> Curry<C, T1, R>( Func<C, object[], R> f, C c ) =>
        t1 => f( c, [t1] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, R> Curry<C, T1, T2, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2 ) => f( c, [t1, t2] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, R> Curry<C, T1, T2, T3, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3 ) => f( c, [t1, t2, t3] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, R> Curry<C, T1, T2, T3, T4, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4 ) => f( c, [t1, t2, t3, t4] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, R> Curry<C, T1, T2, T3, T4, T5, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5 ) => f( c, [t1, t2, t3, t4, t5] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, R> Curry<C, T1, T2, T3, T4, T5, T6, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6 ) => f( c, [t1, t2, t3, t4, t5, t6] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7 ) => f( c, [t1, t2, t3, t4, t5, t6, t7] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R>( Func<C, object[], R> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16] );
}

internal static class CurryAction
{
    public static readonly MethodInfo[] Methods = typeof( CurryAction ).GetMethods();

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action Curry<C>( Action<C, object[]> f, C c ) =>
        () => f( c, [] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1> Curry<C, T1>( Action<C, object[]> f, C c ) =>
        t1 => f( c, [t1] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2> Curry<C, T1, T2>( Action<C, object[]> f, C c ) =>
        ( t1, t2 ) => f( c, [t1, t2] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3> Curry<C, T1, T2, T3>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3 ) => f( c, [t1, t2, t3] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4> Curry<C, T1, T2, T3, T4>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4 ) => f( c, [t1, t2, t3, t4] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5> Curry<C, T1, T2, T3, T4, T5>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5 ) => f( c, [t1, t2, t3, t4, t5] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6> Curry<C, T1, T2, T3, T4, T5, T6>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6 ) => f( c, [t1, t2, t3, t4, t5, t6] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7> Curry<C, T1, T2, T3, T4, T5, T6, T7>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7 ) => f( c, [t1, t2, t3, t4, t5, t6, t7] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15] );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>( Action<C, object[]> f, C c ) =>
        ( t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16 ) => f( c, [t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16] );
}
