using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Hyperbee.Xs.Extensions.Lab;

public class RegexMatchExpression : Expression
{
    public Expression InputExpression { get; }
    public Expression Pattern { get; }

    public RegexMatchExpression( Expression inputExpression, Expression pattern )
    {
        InputExpression = inputExpression ?? throw new ArgumentNullException( nameof( inputExpression ) );
        Pattern = pattern ?? throw new ArgumentNullException( nameof( pattern ) );
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof( MatchCollection );
    public override bool CanReduce => true;

    protected override Expression VisitChildren( ExpressionVisitor visitor )
    {
        var visitedInput = visitor.Visit( InputExpression );
        var visitedPattern = visitor.Visit( Pattern );

        if ( visitedInput != InputExpression || visitedPattern != Pattern )
        {
            return new RegexMatchExpression( visitedInput, visitedPattern );
        }

        return this;
    }

    public override Expression Reduce()
    {
        var regexMatchesMethod = typeof( Regex )
            .GetMethod( nameof( Regex.Matches ), [typeof( string )] )!;

        // Use a constructor expression to create the Regex instance
        var regexConstructor = typeof( Regex ).GetConstructor( [typeof( string )] )!;

        return Call(
            New( regexConstructor, Pattern ),
            regexMatchesMethod,
            InputExpression
        );
    }
}

public static partial class ExpressionExtensions
{
    public static RegexMatchExpression Regex( Expression inputExpression, Expression pattern )
    {
        return new RegexMatchExpression( inputExpression, pattern );
    }
}
