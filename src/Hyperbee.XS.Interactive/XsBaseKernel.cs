using System.Linq.Expressions;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
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
        var referenceManager = new ReferenceManager();
        var typeResolver = TypeResolver.Create( referenceManager );

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
                new PackageParseExtension()
            ]
        };

        Scope.EnterScope( FrameType.Method );

        RegisterForDisposal( () =>
        {
            Scope.ExitScope();
            Values = [];
        } );
    }


    protected static BlockExpression WrapWithPersistentState(
        Expression userExpression,
        IDictionary<string, ParameterExpression> symbols,
        Dictionary<string, object> values )
    {
        var localVariables = new Dictionary<string, ParameterExpression>();
        var initExpressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var valuesConst = Constant( values );
        var indexerProperty = typeof( Dictionary<string, object> ).GetProperty( "Item" )!;

        foreach ( var (name, parameter) in symbols )
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

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly Dictionary<string, ParameterExpression> _locals;
        public ParameterReplacer( Dictionary<string, ParameterExpression> locals ) => _locals = locals;
        protected override Expression VisitParameter( ParameterExpression node ) =>
            node.Name != null && _locals.TryGetValue( node.Name, out var replacement )
                ? replacement
                : base.VisitParameter( node );
    }
}
