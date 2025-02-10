using System.Collections.ObjectModel;
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

                if ( xsContext.Debugger == null || xsContext.Debugger.BreakMode != BreakMode.Call )
                    return Empty();

                var position = xsContext.Scanner.Cursor.Position;
                var tryBreak = xsContext.Debugger.TryBreak;

                var target = tryBreak.Target != null
                    ? Constant( tryBreak.Target )
                    : null;

                var debugExpression = Call(
                    target,
                    tryBreak.Method,
                    Constant( position.Line ),
                    Constant( position.Column ),
                    XsParsersHelper.CaptureVariables( xsContext.Scope.Variables ),
                    Constant( BufferHelper.GetLine( xsContext.Scanner.Buffer, position.Offset ).ToString() )
                );

                return (condition != null)
                    ? IfThen( condition, debugExpression )
                    : debugExpression;
            } );
    }
}
