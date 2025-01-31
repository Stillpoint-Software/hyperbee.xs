﻿using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Writer;
using Parlot.Fluent;

namespace Hyperbee.Xs.Extensions;

public class AwaitParseExtension : IParseExtension, IExtensionWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "await";


    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;

        return expression
            .Then<Expression>( static parts => ExpressionExtensions.Await( parts ) )
            .Named( "await" );
    }

    public bool CanWrite( Expression node )
    {
        return node is AwaitExpression awaitExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not AwaitExpression awaitExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.Await", true, false );

        writer.WriteExpression( awaitExpression.Target );
        if ( awaitExpression.ConfigureAwait )
        {
            writer.Write( ",\n" );
            writer.Write( "true", indent: true );
        }
    }
}
