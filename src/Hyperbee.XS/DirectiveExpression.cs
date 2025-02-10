using System.Linq.Expressions;

namespace Hyperbee.XS;

public class DirectiveExpression : Expression
{
    public string Directive { get; init; }
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof( void );
    public override bool CanReduce => true;

    public DirectiveExpression( string directive )
    {
        Directive = directive;
    }

    public override Expression Reduce()
    {
        return Empty();
    }
}

public static partial class XsExpressionExtensions
{
    public static DirectiveExpression Directive( string directive )
    {
        return new DirectiveExpression( directive );
    }
}
