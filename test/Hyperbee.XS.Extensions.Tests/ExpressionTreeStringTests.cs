using System.Linq.Expressions;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Writer;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class ExpressionTreeStringTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    public ExpressionVisitorConfig Config = new( "Expression.", "\t", "expression",
            XsExtensions.Extensions().OfType<IExpressionWriter>().ToArray() );

    public XsVisitorConfig XsConfig = new( "\t",
            XsExtensions.Extensions().OfType<IXsWriter>().ToArray() );

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_ForLoop()
    {
        var script = """
            var x = 1;
            for( var i = 0; i < 10; i++ )
            {
                x += i;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_ForEachLoop()
    {
        var script = """
            var array = new int[] { 1,2,3 };
            var x = 0;
            foreach ( var item in array )
            {
                x = x + item;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_While()
    {
        var script = """
            var running = true;
            var x = 0;
            while ( running )
            {    
                x++;
                if ( x == 10 )
                { 
                    running = false;
                }
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_Using()
    {
        var script = """
            var x = 0;
            var onDispose = () => { x++; };
            using( var disposable = new Hyperbee.XS.Extensions.Tests.Disposable(onDispose) )
            {
                x++;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_EnumerableBlock()
    {
        var script = """
                     enumerable {
                         yield 1;
                         yield 2;
                         halt;
                         yield 3;
                     }
                     """;


        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<IEnumerable<int>>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValueEnumerable( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_StringFormat()
    {
        var script = """
            var x = "hello";
            var y = "!";
            var result = `{x} world{y}`;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionTreeString_ShouldCreate_AsyncAwait()
    {
        var t = await Task.FromResult( 42 );

        var script = """
            async {
                var asyncBlock = async {
                    await Task.FromResult( 42 );
                };

                await asyncBlock;
            }
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString( Config );

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<Task<int>>>( expression );
        var compiled = lambda.Compile();
        var result = await compiled();

        await AssertScriptValueAsync( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_AsyncAwait()
    {
        var t = await Task.FromResult( 42 );

        var script = """
            async {
                var asyncBlock = async {
                    await Task.FromResult( 42 );
                };

                await asyncBlock;
            }
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<Task<int>>>( newExpression );
        var compiled = lambda.Compile();
        var result = await compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValueAsync( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_ForLoop()
    {
        var script = """
            var x = 1;
            for( var i = 0; i < 10; i++ )
            {
                x += i;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<int>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_ForEachLoop()
    {
        var script = """
            var array = new int[] { 1,2,3 };
            var x = 0;
            foreach ( var item in array )
            {
                x = x + item;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<int>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_While()
    {
        var script = """
            var running = true;
            var x = 0;
            while ( running )
            {    
                x++;
                if ( x == 10 )
                { 
                    running = false;
                }
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<int>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_Using()
    {
        var script = """
            var x = 0;
            var onDispose = () => { x++; };
            using( var disposable = new Hyperbee.XS.Extensions.Tests.Disposable(onDispose) )
            {
                x++;
            }
            x;
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<int>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_EnumerableBlock()
    {
        var script = """
                     enumerable {
                         yield 1;
                         yield 2;
                         halt;
                         yield 3;
                     }
                     """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<IEnumerable<int>>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValueEnumerable( code, result );
    }

    [TestMethod]
    public async Task ToXsString_ShouldCreate_StringFormat()
    {
        var script = """
            var x = "hello";
            var y = "!";
            var result = `{x} world{y}`;
            result;
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<string>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        var code = expression.ToExpressionString( Config );
        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public void ToXsString_ShouldCreate_WithExtensions()
    {
        const string script =
            """
            package Humanizer.Core;
            using Humanizer;
            
            var number = 123;
            number.ToWords();
            """;

        var expression = Xs.Parse( script );
        var newScript = expression.ToXS( XsConfig );

        WriteResult( script, newScript );

        var newExpression = Xs.Parse( newScript );
        var lambda = Expression.Lambda<Func<string>>( newExpression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "one hundred and twenty-three", result );
    }

    public static async Task AssertScriptValue<T>( string code, T result )
    {
        var scriptOptions = ScriptOptions.Default.WithReferences(
            [
                "System",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Collections",
                "System.Collections.Generic",
                "Hyperbee.XS.Extensions.Tests"
            ]
         );

        var name = typeof( T ).Name;

        var scriptResult = await CSharpScript.EvaluateAsync<T>(
            code +
            $"var lambda = Expression.Lambda<Func<{name}>>( expression );" +
            "var compiled = lambda.Compile();" +
            "return compiled();", scriptOptions );

        Assert.AreEqual( result, scriptResult );
    }

    public static async Task AssertScriptValueEnumerable( string code, IEnumerable<int> result )
    {
        var scriptOptions = ScriptOptions.Default.WithReferences(
            [
                "System",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Collections",
                "System.Collections.Generic",
                "Hyperbee.XS.Extensions.Tests"
            ]
        );

        var scriptResult = await CSharpScript.EvaluateAsync<IEnumerable<int>>(
            code +
            $"var lambda = Expression.Lambda<Func<IEnumerable<int>>>( expression );" +
            "var compiled = lambda.Compile();" +
            "return compiled();", scriptOptions );

        Assert.IsTrue( result.SequenceEqual( scriptResult ) );
    }

    public static async Task AssertScriptValueAsync<T>( string code, T result )
    {
        var scriptOptions = ScriptOptions.Default.WithReferences(
            [
                "System",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Collections",
                "System.Collections.Generic",
                "Hyperbee.Expressions",
                "Hyperbee.XS.Extensions.Tests"
            ]
         );
        var name = typeof( T ).Name;

        var scriptResult = await CSharpScript.EvaluateAsync<T>(
            code +
            $"var lambda = Expression.Lambda<Func<Task<{name}>>>( expression );" +
            "var compiled = lambda.Compile();" +
            "return await compiled();", scriptOptions );

        Assert.AreEqual( result, scriptResult );
    }

    private static void WriteResult( string script, string code )
    {
#if DEBUG
        Console.WriteLine( "Script:" );
        Console.WriteLine( script );

        Console.WriteLine( "\nCode:" );
        Console.WriteLine( code );
#endif
    }
}
