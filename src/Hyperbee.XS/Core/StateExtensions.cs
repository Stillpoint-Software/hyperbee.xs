using System.Linq.Expressions;
using Hyperbee.Collections;

namespace Hyperbee.XS.Core;

public static class StateExtensions
{
    public static Expression ParseWithState( this XsParser parser, string script, ParseScope scope, Dictionary<string, object> state )
    {
        // push new local scope
        scope.Variables.Push();

        return parser
            .Parse( script, null, scope )
            .WithState( scope, state );
    }

    public static object InvokeWithState( this Expression expression, ParseScope scope )
    {
        var delegateType = expression.Type == typeof( void )
            ? typeof( Action )
            : typeof( Func<> ).MakeGenericType( expression.Type );

        var result = Expression
            .Lambda( delegateType, expression )
            .Compile()
            .DynamicInvoke();

        scope.PopWithState();

        return result;
    }

    private static BlockExpression WithState(
        this Expression userExpression,
        ParseScope scope,
        Dictionary<string, object> state)
    {
        var localVariables = new Dictionary<string, ParameterExpression>();
        var initExpressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var stateConst = Expression.Constant(state);
        var indexerProperty = typeof(Dictionary<string, object>).GetProperty("Item")!;

        foreach (var (name, parameter) in scope.Variables.EnumerateItems(LinkedNode.Single))
        {
            var local = Expression.Variable(parameter.Type, name);
            localVariables[name] = local;

            var keyExpr = Expression.Constant(name);

            // Assign the local variable to the value from the dictionary if it exists and is the correct type.
            initExpressions.Add(
                (state.TryGetValue(name, out var value) && value.GetType() == parameter.Type)
                    ? Expression.Assign(local, Expression.Convert(Expression.Property(stateConst, indexerProperty, keyExpr), parameter.Type))
                    : Expression.Assign(local, Expression.Default(parameter.Type))
            );

            var localAsObject = parameter.Type.IsValueType
                ? Expression.Convert(local, typeof(object))
                : (Expression)local;

            updateExpressions.Add(
                Expression.Assign(Expression.Property(stateConst, indexerProperty, keyExpr), localAsObject)
            );
        }

        var replacer = new ParameterReplacer(localVariables);

        // Capture the user expression result and wrap in a try-finally block.
        var tryBlock = replacer.Visit(userExpression);

        // remove variables from top level block
        if (tryBlock is BlockExpression block)
            tryBlock = Expression.Block(block.Expressions);

        var tryFinally = Expression.TryFinally(tryBlock, Expression.Block(updateExpressions));

        // Create the wrapping block.
        var blockExpressions = new List<Expression>();
        blockExpressions.AddRange(initExpressions);
        blockExpressions.Add(tryFinally);

        return Expression.Block(
            localVariables.Values,
            blockExpressions
        );
    }

    private static void PopWithState( this ParseScope scope )
    {
        var newVariables = scope.Variables.Pop().Dictionary;
        foreach ( var variable in newVariables )
        {
            scope.Variables[variable.Key] = variable.Value;
        }
    }

    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly Dictionary<string, ParameterExpression> _locals;

        public ParameterReplacer(Dictionary<string, ParameterExpression> locals)
        {
            _locals = locals;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node.Name != null && _locals.TryGetValue(node.Name, out var replacement)
                ? replacement
                : base.VisitParameter(node);
    }
}
