﻿using Hyperbee.XS.Core;

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
            new PackageParseExtension()
        ];
    }
}
