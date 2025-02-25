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

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 11, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGotoCatch()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                goto label2; 
            }
            catch(ArgumentException)
            {
                label2:
                x = 42;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGotoFinally()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                goto label3; 
            }
            catch(ArgumentException)
            {
                label2:
                x = 21;
            }
            finally
            {
                label3:
                x = 42;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }
}

