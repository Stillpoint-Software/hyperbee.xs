﻿using System.Linq.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Hyperbee.XS.System.Parsers.XsParsers;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class DebugParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Terminated;
    public string Key => "debug";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;

        return Between(
                Terms.Char( '(' ),
                ZeroOrOne( expression ),
                Terms.Char( ')' )
            )
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( context, condition ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

                if ( xsContext.Debugger == null )
                    return Empty();

                var span = context.Scanner.Cursor.Position;
                var captureVariables = CaptureVariables( xsContext.Scope.Variables );
                var frame = xsContext.Scope.Frame;

                var debugger = xsContext.Debugger;
                var target = debugger.Target != null
                    ? Constant( debugger.Target )
                    : null;

                var debugExpression = Call(
                    target,
                    debugger.Method,
                    Constant( span.Line ),
                    Constant( span.Column ),
                    captureVariables,
                    Constant( frame )
                );

                return (condition != null)
                    ? IfThen( condition, debugExpression )
                    : debugExpression;
            } );
    }
}
