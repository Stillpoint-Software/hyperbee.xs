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
        new XsConfig
        {
            ReferenceManager = ReferenceManager.Create( Assembly.GetExecutingAssembly() ),
            Extensions = [new AnswerToEverythingParseExtension()]
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse( "answer; // answer to everything" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
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

