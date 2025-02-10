using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Collections;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Core.Parsers;

public static partial class XsParsers
{
    public static Parser<Expression> Debuggable( this Parser<Expression> parser )
    {
        return parser.Then( ( context, statement ) =>
        {
            if ( context is not XsContext xsContext )
                throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

            if ( xsContext.Debugger == null || xsContext.Debugger.BreakMode != BreakMode.Statements )
                return statement;

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

            return Block( debugExpression, statement );
        } );
    }
}

public static class XsParsersHelper
{
    private static readonly MethodInfo AddMethod;
    private static readonly ConstructorInfo Constructor;

    static XsParsersHelper()
    {
        AddMethod = typeof( Dictionary<string, object> ).GetMethod( "Add", BindingFlags.Instance | BindingFlags.Public, [typeof( string ), typeof( object )] );
        Constructor = typeof( Dictionary<string, object> ).GetConstructor( Type.EmptyTypes )!;
    }

    public static Expression CaptureVariables( LinkedDictionary<string, ParameterExpression> variables )
    {
        return ListInit(
            New( Constructor ),
            variables.Select(
                kvp => ElementInit(
                    AddMethod,
                    Constant( kvp.Key ),
                    Convert( kvp.Value, typeof( object ) )
                )
            )
        );
    }
}
