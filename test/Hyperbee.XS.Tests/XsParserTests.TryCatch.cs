using Hyperbee.XS.Core.Writer;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserTryCatchTests
{
    public static XsParser Xs { get; } = new();

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithTryCatch( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithTryFinally( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithTryCatchFinally( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldCatchException( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldHandleMultipleCatchBlocks( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNestedTryCatch( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldRethrowException( CompilerType compiler )
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

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldThrowNewException( CompilerType compiler )
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
                    throw new ArgumentException();
                }
            }
            catch(ArgumentException)
            {
                x += 10;
            }
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldThrowWithOutTry( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            
            throw new ArgumentException("Argument Error");

            x = 42;
            x;
            """
        );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );

        try
        {
            var result = function();
            Assert.Fail( "Expected exception" );
        }
        catch ( InvalidOperationException ioex )
        {
            Assert.AreEqual( "Argument Error", ioex.InnerException.Message );
        }
    }

}

