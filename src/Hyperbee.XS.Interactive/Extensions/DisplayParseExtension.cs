using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Microsoft.DotNet.Interactive;
using Parlot.Fluent;

using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Interactive.Extensions;

public class DisplayParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    private static readonly MethodInfo DisplayMethodInfo = typeof( KernelInvocationContextExtensions )
        .GetMethod( nameof( KernelInvocationContextExtensions.Display ),
            [
                typeof( KernelInvocationContext ),
                typeof( object ),
                typeof( string[] )
            ] );

    public ExtensionType Type => ExtensionType.Terminated;

    public string Key => "display";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return Between(
                    Terms.Char( '(' ),
                    ZeroOrOne( Separated( Terms.Char( ',' ), expression ), [] ),
                    Terms.Char( ')' )
                )
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( ( ctx, expressions ) =>
            {
                var parts = expressions?.ToArray();

                if ( parts == null || parts.Length == 0 )
                    throw new SyntaxException( "display requires at least one argument." );

                var paramArray = new Expression[parts.Length - 1];

                for ( var i = 1; i < parts.Length; i++ )
                {
                    if ( parts[i].Type != typeof( string ) )
                        throw new SyntaxException( "display mimetypes must be strings." );

                    paramArray[i - 1] = parts[i];
                }

                return Call( null, DisplayMethodInfo,
                    Constant( KernelInvocationContext.Current ),
                    Convert( parts[0], typeof( object ) ),
                    NewArrayInit( typeof( string ), paramArray )
                );

            } );
    }

    public bool CanWrite( Expression node )
    {
        return node is DirectiveExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context ) { }

    public void WriteExpression( Expression node, XsWriterContext context ) { }
}

