using System.Linq.Expressions;
using Hyperbee.Collections;
using Hyperbee.Xs.Extensions;
using Hyperbee.Xs.Interactive.Extensions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Microsoft.DotNet.Interactive;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Interactive;

public class XsBaseKernel : Kernel
{
    protected XsConfig Config;
    protected ParseScope Scope = new();
    protected Dictionary<string, object> Values = [];

    public XsBaseKernel( string name ) : base( name )
    {
        var typeResolver = TypeResolver.Create( 
            typeof( object ).Assembly,
            typeof( Enumerable ).Assembly,
            typeof( DisplayExtensions ).Assembly // pull in .NET Interactive helpers?
        );

        Config = new XsConfig( typeResolver )
        {
            Extensions = [
                new StringFormatParseExtension(),
                new ForEachParseExtension(),
                new ForParseExtension(),
                new WhileParseExtension(),
                new UsingParseExtension(),
                new AsyncParseExtension(),
                new AwaitParseExtension(),
                new PackageParseExtension(),

                // Notebook Helpers
                new DisplayParseExtension()
            ]
        };

        Scope.EnterScope( FrameType.Method );

        RegisterForDisposal( () =>
        {
            Scope.ExitScope();
            Scope = null;
            Values = null;
            Config = null;
        } );
    }

    protected static BlockExpression WrapWithPersistentState(
        Expression userExpression,
        LinkedDictionary<string, ParameterExpression> symbols,
        Dictionary<string, object> values )
    {
        var localVariables = new Dictionary<string, ParameterExpression>();
        var initExpressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var valuesConst = Constant( values );
        var indexerProperty = typeof( Dictionary<string, object> ).GetProperty( "Item" )!;

        foreach ( var (name, parameter) in symbols.EnumerateItems( LinkedNode.Single ) )
        {
            var local = Variable( parameter.Type, name );
            localVariables[name] = local;

            var keyExpr = Constant( name );

            initExpressions.Add(
                values.ContainsKey( name )
                    ? Assign( local, Convert( Property( valuesConst, indexerProperty, keyExpr ), parameter.Type ) )
                    : Assign( local, Default( parameter.Type ) )
            );

            var localAsObject = parameter.Type.IsValueType
                ? Convert( local, typeof( object ) )
                : (Expression) local;

            updateExpressions.Add(
                Assign( Property( valuesConst, indexerProperty, keyExpr ), localAsObject )
            );
        }

        var replacer = new ParameterReplacer( localVariables );

        // Capture the user expression result and wrap in a try-finally block.
        var tryBlock = replacer.Visit( userExpression );

        // remove variables from top level block
        if ( tryBlock is BlockExpression block )
            tryBlock = Block( block.Expressions );

        var tryFinally = TryFinally( tryBlock, Block( updateExpressions ) );

        // Create the wrapping block.
        var blockExpressions = new List<Expression>();
        blockExpressions.AddRange( initExpressions );
        blockExpressions.Add( tryFinally );

        return Block(
            localVariables.Values,
            blockExpressions
        );
    }

    private class ParameterReplacer( Dictionary<string, ParameterExpression> locals ) : ExpressionVisitor
    {
        protected override Expression VisitParameter( ParameterExpression node ) =>
            node.Name != null && locals.TryGetValue( node.Name, out var replacement )
                ? replacement
                : base.VisitParameter( node );
    }
}
