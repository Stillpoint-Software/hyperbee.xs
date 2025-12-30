using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserGotoTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithGotoStatements( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 11, result );
    }
}

