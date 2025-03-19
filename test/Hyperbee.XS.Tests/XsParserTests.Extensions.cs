using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.Core;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExtensionsTests
{
    public static XsParser Xs { get; set; } = new
    (
        new XsConfig( new TypeResolver( ReferenceManager.Create( Assembly.GetExecutingAssembly() ) ) )
        {
            Extensions = [new AnswerToEverythingParseExtension(), new RepeatParseExtension()]
        }
    );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithExtensions( CompilerType compiler )
    {
        var expression = Xs.Parse( "answer; // answer to everything" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithRepeatExtension()
    {
        var expression = Xs.Parse( """
            var x = 0;
            repeat (5) {
                x++;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result );
    }
}

public class AnswerToEverythingParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "answer";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        return Always()
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( static ( _, _ ) => Constant( 42 ) )
            .Named( "hitchhiker" );
    }
}

public class RepeatExpression : Expression
{
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof( void );
    public override bool CanReduce => true;

    public Expression Count { get; }
    public Expression Body { get; }

    public RepeatExpression( Expression count, Expression body )
    {
        Count = count;
        Body = body;
    }

    public override Expression Reduce()
    {
        var loopVariable = Parameter( typeof( int ), "i" );
        var breakLabel = Label();

        return Block(
            [loopVariable],
            Assign( loopVariable, Constant( 0 ) ),
            Loop(
                IfThenElse(
                    LessThan( loopVariable, Count ),
                    Block( Body, PostIncrementAssign( loopVariable ) ),
                    Break( breakLabel )
                ),
                breakLabel
            )
        );
    }
}

public class RepeatParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "repeat";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return Between(
            Terms.Char( '(' ),
            expression,
            Terms.Char( ')' )
        )
        .And(
             Between(
                Terms.Char( '{' ),
                statement,
                Terms.Char( '}' )
            )
        )
        .Then<Expression>( static parts =>
        {
            var (countExpression, body) = parts;
            return new RepeatExpression( countExpression, body );
        } );
    }
}

