using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserGotoTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_WithGotoStatements()
    {
        var expression = Xs.Parse(
            """
            label1:
                var x = 10;
                if (x > 5) {
                    goto label2;
                }
                x = 0;
            label2:
                x += 1;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 11, result );
    }
}

