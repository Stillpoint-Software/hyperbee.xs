using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class AsyncParseExtensionTests
{
    public static XsParser Xs { get; set; } = new
    (
        new XsConfig
        {
            ReferenceManager = ReferenceManager.Create( Assembly.GetExecutingAssembly() ),
            Extensions = XsExtensions.Extensions()
        }
    );

    [TestMethod]
    public async Task Compile_ShouldSucceed_WithAsyncBlock()
    {
        var expression = Xs.Parse(
            """
            async {
                await Task.FromResult( 42 );
            }
            """ );

        var lambda = Lambda<Func<Task<int>>>( expression );

        var compiled = lambda.Compile();
        var result = await compiled();

        Assert.AreEqual( 42, result );
    }

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
    }
}



