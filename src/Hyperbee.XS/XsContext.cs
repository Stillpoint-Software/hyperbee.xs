﻿using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Hyperbee.XS.System;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS;

public class XsContext : ParseContext
{
    public TypeResolver Resolver { get; }
    public ParseScope Scope { get; } = new();
    public List<string> Namespaces { get; } = [];

    public bool RequireTermination { get; set; } = true;


#if DEBUG
    public Stack<object> ParserStack { get; } = new();
#endif

    public XsContext( XsConfig config, Scanner scanner, bool useNewLines = false )
        : base( scanner, useNewLines )
    {
        Resolver = config.Resolver.Value;

#if DEBUG
        OnEnterParser += ( obj, ctx ) =>
        {
            ParserStack.Push( obj );
        };

        OnExitParser += ( obj, ctx ) =>
        {
            if ( ParserStack.Peek() == obj )
                ParserStack.Pop();
        };
#endif
    }

    public void Deconstruct( out ParseScope scope, out TypeResolver resolver )
    {
        scope = Scope;
        resolver = Resolver;
    }

    public void Deconstruct( out ParseScope scope, out TypeResolver resolver, out Frame frame )
    {
        scope = Scope;
        resolver = Resolver;
        frame = Scope.Frame;
    }
}

public static class ParseContextExtensions
{
    public static void Deconstruct( this ParseContext context, out ParseScope scope, out TypeResolver resolver )
    {
        if ( context is XsContext xsContext )
        {
            (scope, resolver) = xsContext;
            return;
        }

        scope = default;
        resolver = default;
    }

    public static void Deconstruct( this ParseContext context, out ParseScope scope, out TypeResolver resolver, out Frame frame )
    {
        if ( context is XsContext xsContext )
        {
            (scope, resolver, frame) = xsContext;
            return;
        }

        scope = default;
        resolver = default;
        frame = default;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static ParseScope Scope( this ParseContext context )
    {
        if ( context is not XsContext xsContext )
            throw new NotImplementedException( "Context does not implement Scope." );

        return xsContext.Scope;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static void EnterScope( this ParseContext context, FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        context.Scope().EnterScope( frameType, breakLabel, continueLabel );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static void ExitScope( this ParseContext context )
    {
        context.Scope().ExitScope();
    }
}

