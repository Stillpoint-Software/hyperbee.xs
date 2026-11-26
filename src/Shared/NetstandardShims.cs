#if NETSTANDARD

// Everything netstandard2.1 needs from the compiler that newer targets have in box.
// Hand-written rather than taken from the Polyfill package: Polyfill compiles all of
// its internal polyfills into the consuming assembly and the compiler does not strip
// unused internal types, which measured at roughly 320,000 bytes against a few
// thousand for these shims. This repo packs four assemblies, so that is close to a
// megabyte of dead code across the netstandard2.1 assets. Only what is listed here is
// actually used, and all of it is small and stable.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Marker the compiler requires to emit an <c>init</c> accessor.
    /// </summary>
    /// <remarks>
    /// The compiler only needs this type to exist by name, so an empty internal class
    /// is the whole contract. Added in .NET 5.
    /// </remarks>
    internal static class IsExternalInit;

    /// <summary>
    /// Lets a parameter default to the caller's source text for another argument.
    /// </summary>
    /// <remarks>
    /// The compiler reads the parameter name and fills the argument in at the call site,
    /// so, as with <see cref="IsExternalInit"/>, only the shape matters. Added in .NET 6.
    /// </remarks>
    [AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false, Inherited = false )]
    internal sealed class CallerArgumentExpressionAttribute( string parameterName ) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}


namespace System.Collections.Generic
{
    /// <summary>
    /// Stands in for the <c>ICollection&lt;T&gt;.AddRange</c> extensions added to
    /// <c>System.Collections.Generic.CollectionExtensions</c> in .NET 10.
    /// </summary>
    /// <remarks>
    /// Declared in the framework's own namespace so implicit usings bring it into
    /// scope at every call site with no source change. It is compiled only for
    /// netstandard, so on net10.0 and net11.0 the framework method is the single
    /// candidate and there is no CS0121 ambiguity.
    /// </remarks>
    internal static class CollectionAddRangeExtensions
    {
        public static void AddRange<T>( this ICollection<T> collection, IEnumerable<T> source )
        {
            if ( collection is null )
                throw new ArgumentNullException( nameof( collection ) );

            if ( source is null )
                throw new ArgumentNullException( nameof( source ) );

            foreach ( var item in source )
                collection.Add( item );
        }
    }
}

#endif
