using System.Runtime.CompilerServices;

// Enabled per file, not per project: this source is compiled into projects on
// both sides of the <Nullable> setting, so the annotations below have to be
// valid in either context.
#nullable enable

namespace Hyperbee.XS.Shared;

/// <summary>
/// Argument guards that keep the framework's own behavior on modern targets and
/// supply an equivalent on netstandard2.1.
/// </summary>
/// <remarks>
/// <para>
/// netstandard2.1 has no <c>ArgumentNullException.ThrowIfNull</c>, which arrived in
/// .NET 6. It is a static member on a framework type, so no extension method and no
/// polyfill package can supply it: C# cannot add a static member to an existing type.
/// Routing the call sites through this class is the only way to keep one source tree.
/// </para>
/// <para>
/// On every target that has it, this method forwards to the framework, so behavior
/// -- the exception type, the message, and the JIT's treatment of the null check --
/// is unchanged. The netstandard2.1 branch exists only to stand in where the
/// framework method is absent.
/// </para>
/// <para>
/// A shim class shadowing <see cref="ArgumentNullException"/> was rejected: it would
/// have to derive from the real exception, and exact-type exception assertions in
/// consumer tests would then fail.
/// </para>
/// <para>
/// This file is compiled into each project that needs it rather than exposed from
/// <c>Hyperbee.XS</c>, so it adds nothing to any assembly's public surface.
/// </para>
/// </remarks>
internal static class ThrowIf
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.
    /// </summary>
    public static void Null(
        object? value,
        [CallerArgumentExpression( nameof( value ) )] string? paramName = null )
    {
#if NETSTANDARD
        if ( value is null )
            throw new ArgumentNullException( paramName );
#else
        ArgumentNullException.ThrowIfNull( value, paramName );
#endif
    }
}
