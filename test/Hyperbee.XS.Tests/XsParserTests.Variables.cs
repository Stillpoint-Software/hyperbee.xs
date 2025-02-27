using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserVariableTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariable()
    {
        var expression = Xs.Parse( "var x = 10;" );

        var lambda = Lambda( expression );

        var function = lambda.CompileEx( preferInterpret: true );

        Assert.IsNotNull( function );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndResult()
    {
        var expression = Xs.Parse( "var x = 10; x + 10;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndAssignmentResult()
    {
        var expression = Xs.Parse( "var x = 10; x = x + 10; x + 22;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableShiftLeftResult()
    {
        var expression = Xs.Parse( "var x = 32; x <<= 2; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 8, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableShiftRightResult()
    {
        var expression = Xs.Parse( "var x = 8; x >>= 2; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 32, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableComplementResult()
    {
        var expression = Xs.Parse(
            """
            var x = 8;
            x + (~x) + 1;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 0, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPostResult()
    {
        var expression = Xs.Parse( "var x = 10; x++;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 10, result ); // x++ returns the value before increment
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPrefixResult()
    {
        var expression = Xs.Parse( "var x = 10; ++x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 11, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAddAssignment()
    {
        var expression = Xs.Parse( "var x = 10; x += 32; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableSubtractAssignment()
    {
        var expression = Xs.Parse( "var x = 42; x -= 32; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableMultiplyAssignment()
    {
        var expression = Xs.Parse( "var x = 10; x *= 4; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 40, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableDivideAssignment()
    {
        var expression = Xs.Parse( "var x = 40; x /= 4; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableExponentAssignment()
    {
        var expression = Xs.Parse( "var x = 2; x **= 3; x;" );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 8, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyAssignment()
    {

        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(21);
            x.PropertyValue *= 2;
            x.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyComplexAssignment()
    {

        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(5);
            x.PropertyValue *= (x.MethodValue() + 5) - 2;
            x.MethodValue() + 2;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithIndexerAssignment()
    {

        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x[1] = 1;
            x.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldAllowDoubleAssignment()
    {
        const string xs = "var x = var y = 42; x;";

        var expression = Xs.Parse( xs );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithDefaultInvalid()
    {
        try
        {
            Xs.Parse( "var x ~ 10;" );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }
}
