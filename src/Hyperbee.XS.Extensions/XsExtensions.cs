using Hyperbee.XS.Core;

namespace Hyperbee.Xs.Extensions;

public static class XsExtensions
{
    public static IReadOnlyCollection<IParseExtension> Extensions()
    {
        return [
            new StringFormatParseExtension(),
            new ForEachParseExtension(),
            new ForParseExtension(),
            new WhileParseExtension(),
            new UsingParseExtension(),
            new AsyncParseExtension(),
            new AwaitParseExtension(),
            new DebugParseExtension(),
#if !NETSTANDARD
            // PackageParseExtension resolves NuGet packages through
            // ReferenceManager.LoadPackageAsync, which the netstandard2.1 asset does
            // not carry. See src/Hyperbee.XS/Core/ReferenceManager.cs.
            new PackageParseExtension(),
#endif
            new EnumerableParseExtension(),
            new YieldBreakParseExtension(),
            new YieldReturnParseExtension(),
            new InjectParseExtension(),
            new ConfigurationParseExtension()
        ];
    }
}
