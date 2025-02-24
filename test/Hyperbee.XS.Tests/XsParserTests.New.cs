using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserNewTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewExpression()
    {
        var expression = Xs.Parse(
            """
            using Hyperbee.XS.Tests;
            new TestClass(42);
            """ );

        var lambda = Lambda<Func<TestClass>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.PropertyValue );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewAndProperty()
    {
        try
        {
            var expression = Xs.Parse(
                """
                using Hyperbee.XS.Tests;
                new TestClass(42).PropertyThis.PropertyValue;
                """ );

            var lambda = Lambda<Func<int>>( expression );

            var function = lambda.CompileEx( preferInterpret: true );
            var result = function();

            Assert.AreEqual( 42, result );
        }
        catch ( SyntaxException se )
        {
            Assert.Fail( se.Message );
        }
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewArray()
    {
        var expression = Xs.Parse(
            """
            new int[5];
            """ );

        var lambda = Lambda<Func<int[]>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 5, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewMultiDimensionalArray()
    {
        var expression = Xs.Parse(
            """
            new int[2,5];
            """ );

        var lambda = Lambda<Func<int[,]>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 10, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewArrayInit()
    {
        var parser = new XsParser();

        var expression = parser.Parse(
            """
            new int[] {1,2};
            """ );

        var lambda = Lambda<Func<int[]>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 2, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewListInit()
    {
        var parser = new XsParser();

        var expression = parser.Parse(
            """
            new List<int>() {1,2};
            """ );

        var lambda = Lambda<Func<List<int>>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 2, result.Count );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewJaggedArray()
    {
        var parser = new XsParser();

        var expression = parser.Parse(
            """
            new int[] { 
                new int[] {10,20,30}, 
                new int[] {40,50}, 
                new int[] {60} 
            };
            """ );

        var lambda = Lambda<Func<int[][]>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 3, result.Length );
        Assert.AreEqual( 3, result[0].Length );
        Assert.AreEqual( 10, result[0][0] );
        Assert.AreEqual( 2, result[1].Length );
        Assert.AreEqual( 40, result[1][0] );
        Assert.AreEqual( 1, result[2].Length );
        Assert.AreEqual( 60, result[2][0] );

    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGeneric()
    {
        var expression = Xs.Parse(
            """
            new List<int>();
            """ );

        var lambda = Lambda<Func<List<int>>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.IsInstanceOfType<List<int>>( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithDefaultValue()
    {
        var expression = Xs.Parse(
            """
            var x = default( int );
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.AreEqual( 0, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithDefaultReference()
    {
        var expression = Xs.Parse(
            """
            var x = default( Hyperbee.XS.Tests.TestClass );
            x;
            """ );

        var lambda = Lambda<Func<TestClass>>( expression );

        var function = lambda.CompileEx( preferInterpret: true );
        var result = function();

        Assert.IsNull( result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithDefaultInvalid()
    {
        try
        {
            Xs.Parse(
                """
                var x = 5;
                var y = default(wrong};
                x + y;
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidImport()
    {
        try
        {
            Xs.Parse(
            """
            using ;
            new TestClass(42);
            """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidImportMissingIdentifier()
    {
        try
        {
            Xs.Parse(
            """
            using Hyperbee.XS.;
            """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }
}
