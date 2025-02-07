using System.Linq.Expressions;
using Hyperbee.Xs.Extensions.Core;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class NuGetParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Directive;

    public string Key => "nuget";

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

// synchronously execute an async method on the current thread using a custom synchronization context
// https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/

// this method executes all the async operations on the calling thread rather than blocking the calling
// thread while it waits for the results of continuations on the thread pool. this method only ever
// uses the calling thread to manage the async calls vs tying up thread pool threads.
