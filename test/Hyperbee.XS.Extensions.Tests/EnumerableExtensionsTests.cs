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
                halt;
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
    /*
    [TestMethod]
    public async Task Compile_ShouldSucceed_WithAsyncBlockAwait()
    {
        var expression = Xs.Parse(
            """
            async {
                var asyncBlock = async {
                    await Task.FromResult( 42 );
                };

                await asyncBlock;
            }
            """ );

        var lambda = Lambda<Func<Task<int>>>( expression );

        var compiled = lambda.Compile();
        var result = await compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithAsyncBlockGetAwaiter()
    {
        var expression = Xs.Parse(
            """
            var asyncBlock = async {
                await Task.FromResult( 42 );
            };

            await asyncBlock;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public async Task Compile_ShouldSucceed_WithAsyncBlockAwaitVariable()
    {
        var expression = Xs.Parse(
            """
            async {
                var taskVar = Task.FromResult( 40 );
                var asyncBlock = async {
                    var x = 0;
                    var result = await taskVar;
                    x++;
                    result + ++x;
                };

                await asyncBlock;
            }
            """ );

        var lambda = Lambda<Func<Task<int>>>( expression );

        var compiled = lambda.Compile();
        var result = await compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public async Task Compile_ShouldSucceed_WithAsyncBlockLambda()
    {
        var expression = Xs.Parse(
            """
            async {
                var myLambda = () => {
                    async {
                        await Task.FromResult( 42 );
                    }
                };
                await myLambda();
            }
            """ );

        var lambda = Lambda<Func<Task<int>>>( expression );

        var compiled = lambda.Compile();
        var result = await compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGetAwaiter()
    {
        var expression = Xs.Parse(
            """
            await Task.FromResult( 42 ); // GetAwaiter().GetResult();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }*/
}
