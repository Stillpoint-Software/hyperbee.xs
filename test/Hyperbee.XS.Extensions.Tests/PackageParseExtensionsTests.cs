using System.Linq.Expressions;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class PackageParseExtensionTests
{
    public ExpressionVisitorConfig Config = new( "Expression.", "\t", "expression",
            XsExtensions.Extensions().OfType<IExpressionWriter>().ToArray() );

    public XsVisitorConfig XsConfig = new( "\t",
            XsExtensions.Extensions().OfType<IXsWriter>().ToArray() );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        const string script =
            """
            package Humanizer.Core;
            using Humanizer;
            
            var number = 123;
            number.ToWords();
            """;

        var xsConfig = new XsConfig()
        {
            Extensions = [new PackageParseExtension()]
        };

        var xs = new XsParser( xsConfig );
        var expression = xs.Parse( script );

        var lambda = Expression.Lambda<Func<string>>( expression );

        var code = expression.ToExpressionString( Config );
        var xsCode = expression.ToXS( XsConfig );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "one hundred and twenty-three", result );
    }
}
