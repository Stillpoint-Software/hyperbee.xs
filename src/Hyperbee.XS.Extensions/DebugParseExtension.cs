using System.Linq.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
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

                if ( xsContext.DebugInfo == null )
                    return Empty();

                var span = context.Scanner.Cursor.Position;
                var debugInfo = xsContext.DebugInfo;
                var debuggerCallback = debugInfo.Debugger;

                var target = debuggerCallback.Target != null
                    ? Constant( debuggerCallback.Target )
                    : null;

                var debugExpression = Call(
                    target,
                    debuggerCallback.Method,
                    Constant( span.Line ),
                    Constant( span.Column ),
                    XsParsersHelper.CaptureVariables( xsContext.Scope.Variables ),
                    Constant( context.Scanner.Buffer.GetLine( span.Line, span.Column, true ) )
                );

                return (condition != null)
                    ? IfThen( condition, debugExpression )
                    : debugExpression;
            } );
    }
}
