using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class EnumerableExtensionsTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithEnumerableBlock()
    {
        var expression = Xs.Parse(
            """
            enumerable {
                yield 1;
                yield 2;
                yield 3;
            }
            """ );

        var lambda = Lambda<Func<IEnumerable<int>>>( expression );

        var compiled = lambda.Compile();
        var result = compiled().ToArray();

        Assert.AreEqual( 3, result.Length );
        Assert.AreEqual( 1, result[0] );
        Assert.AreEqual( 2, result[1] );
        Assert.AreEqual( 3, result[2] );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithEnumerableBlockBreak()
    {
        var expression = Xs.Parse(
            """
            enumerable {
                yield 1;
                yield 2;
                break;
                yield 3;
            }
            """ );

        var lambda = Lambda<Func<IEnumerable<int>>>( expression );

        var compiled = lambda.Compile();
        var result = compiled().ToArray();

        Assert.AreEqual( 2, result.Length );
        Assert.AreEqual( 1, result[0] );
        Assert.AreEqual( 2, result[1] );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithComplexEnumerable()
    {
        var expression = Xs.Parse(
            """
            enumerable {
                for( var i = 0; i < 3; i++ )
                {
                    yield i;
                }
            }
            """ );

        var lambda = Lambda<Func<IEnumerable<int>>>( expression );

        var compiled = lambda.Compile();
        var result = compiled().ToArray();

        Assert.AreEqual( 3, result.Length );
        Assert.AreEqual( 0, result[0] );
        Assert.AreEqual( 1, result[1] );
        Assert.AreEqual( 2, result[2] );
    }
}
