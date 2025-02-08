﻿using System.Linq.Expressions;
using Hyperbee.Xs.Extensions.Core;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class PackageParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Directive;

    public string Key => "package";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Terms.NamespaceIdentifier()
            .And(
                ZeroOrOne(
                    Terms.Char( ':' ).SkipAnd( Terms.Identifier() )
                )
            )
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( context, parts ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

                var packageId = parts.Item1.ToString();
                var version = parts.Item2.ToString();

                AsyncCurrentThreadHelper.RunSync( async () =>
                {
                    var resolver = xsContext.Resolver;
                    var assemblies = await resolver.ReferenceManager.LoadPackageAsync( packageId, version );
                    resolver.RegisterExtensionMethods( assemblies );
                } );

                return Expression.Empty();
            } );
    }
}

