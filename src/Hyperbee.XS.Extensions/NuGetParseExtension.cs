﻿using System.Collections.Concurrent;
using System.Linq.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class NuGetParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Directive; 
    
    public string Key => "nuget";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Terms.Identifier()
            .And( 
                ZeroOrOne(
                    Terms.Char( ':' ).SkipAnd( Terms.Identifier() )
                )
            )
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( context, parts ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof(XsContext)}." );

                var packageId = parts.Item1.ToString();
                var version = parts.Item2.ToString();

                AsyncCurrentThreadHelper.RunSync( () => xsContext.ReferenceManager.LoadPackageAsync( packageId, version ) );

                return Expression.Empty();
            } );

    }
}

// synchronously execute an async method on the current thread using a custom synchronization context
// https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/

// this method executes all the async operations on the calling thread rather than blocking the calling
// thread while it waits for the results of continuations on the thread pool. this method only ever
// uses the calling thread to manage the async calls vs tying up thread pool threads.

internal static class AsyncCurrentThreadHelper
{
    public static void RunSync( Func<Task> func )
    {
        ArgumentNullException.ThrowIfNull( func );

        var currentContext = SynchronizationContext.Current;
        var syncContext = new SingleThreadSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext( syncContext );
            syncContext.InternalRunSync( func );
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext( currentContext );
        }
    }

    public static T RunSync<T>( Func<Task<T>> func )
    {
        ArgumentNullException.ThrowIfNull( func );

        var currentContext = SynchronizationContext.Current;
        var syncContext = new SingleThreadSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext( syncContext );
            return syncContext.InternalRunSync( func );
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext( currentContext );
        }
    }

    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback callback, object state)> _items = new();

        public void InternalRunSync( Func<Task> func )
        {
            var task = func() ?? throw new InvalidOperationException( "No task provided." );
            task.ContinueWith( _ => Complete(), TaskScheduler.Default ); // final continuation

            RunOnCurrentThread();

            task.GetAwaiter().GetResult();
        }

        public T InternalRunSync<T>( Func<Task<T>> func )
        {
            var task = func() ?? throw new InvalidOperationException( "No task provided." );
            task.ContinueWith( _ => Complete(), TaskScheduler.Default );

            RunOnCurrentThread();

            return task.GetAwaiter().GetResult();
        }

        public override void Post( SendOrPostCallback callback, object state )
        {
            // this method receives posted continuations from the task.
            // we add them to our queue for execution by the run loop. 

            ArgumentNullException.ThrowIfNull( callback );

            _items.Add( (callback, state) );
        }

        public override void Send( SendOrPostCallback callback, object state )
        {
            throw new NotSupportedException( "Synchronous sending is not supported." );
        }

        private void RunOnCurrentThread()
        {
            foreach ( var (callback, state) in _items.GetConsumingEnumerable() )
                callback( state );
        }

        private void Complete() => _items.CompleteAdding();
    }
}
