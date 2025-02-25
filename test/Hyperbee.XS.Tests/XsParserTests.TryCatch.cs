using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserTryCatchTests
{
    public static XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_WithTryCatch()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                x = 42; // do something
            }
            catch(Exception e)
            {
                x -= 10;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithTryFinally()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                x = 10;
            }
            finally
            {
                x += 32;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithTryCatchFinally()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                x = 32; // do something
            }
            catch(Exception e)
            {
                x -= 10;
            }
            finally
            {
                x += 10;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldCatchException()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                throw new InvalidOperationException();
            }
            catch(InvalidOperationException e)
            {
                x = 42;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldHandleMultipleCatchBlocks()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                throw new ArgumentException();
            }
            catch(InvalidOperationException)
            {
                x = 10;
            }
            catch(ArgumentException)
            {
                x = 42;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNestedTryCatch()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch(InvalidOperationException)
                {
                    x = 10;
                }
            }
            finally
            {
                x += 32;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldRethrowException()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            try
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch(InvalidOperationException e)
                {
                    x = 32;
                    throw;
                }
            }
            catch(InvalidOperationException)
            {
                x += 10;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}

