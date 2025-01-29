using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.System.Writer;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Hyperbee.XS.Tests;

[TestClass]
public class ExpressionStringTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig { References = [Assembly.GetExecutingAssembly()] }
    );

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_NestedLambdas()
    {
        var script = """
            var x = 2;
            var y = 3
            var calc = (int a, int b) => {
                return () => a * b;
            };
            calc( x, y )();
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Loops()
    {
        var script = """
            var l = 0;
            loop
            {
                l++; 
                if( l == 42 )
                {
                    break;
                }
            }
            l;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Switch()
    {
        var script = """
            var x = 2;
            var result = 0;
            switch (x)
            {
                case 1:
                    result = 1;
                    break;
                case 2:
                    result = 2;
                    break;
                default:
                    result = -1;
                    break;
            }
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_TryCatch()
    {
        var script = """
            var result = 0;
            try
            {
                var x = 1 / 0;
            }
            catch (Exception)
            {
                result = -1;
            }
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Conditional()
    {
        var script = """
            var x = 5;
            var result = if( x == 5 )
            {   
                1;
            }
            else
            {
                2;
            };
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Goto()
    {
        var script = """
            var result = 0;
            goto Label;
            result = 1;
            Label:
            result = 2;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Return()
    {
        var script = """
            var result = 0;
            return 42;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_InstanceAndSetProperty()
    {
        var script = """
            var instance = new TestClass(0);
            instance.PropertyValue = 10;
            instance.PropertyValue;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );

    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_ArrayInitialization()
    {
        var script = """
            var a = new int[] { 1, 2, 3, 4, 5 };
            a[2];
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_ListInitialization()
    {
        var script = """
            var l = new List<int>() { 1, 2, 3, 4, 5 };
            l[2];
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_CallMethod()
    {
        var script = """
            var instance = new TestClass(5);
            instance.MethodValue();
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_GetIndexer()
    {
        var script = """
            var instance = new TestClass(0);
            instance[5];
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_SetIndexer()
    {
        var script = """
            var instance = new TestClass(0);
            instance[5] = 10;
            instance[5];
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_CallStaticMethod()
    {
        var script = """
            TestClass.StaticAddNumbers(3, 4);
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    public async Task AssertScriptValue<T>( string code, T result )
    {
        var scriptOptions = ScriptOptions.Default.WithReferences(
            [
                "System",
                "System.Linq.Expressions",
                "Hyperbee.XS.Tests"
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

    private void WriteResult( string script, string code )
    {
#if DEBUG
        Console.WriteLine( "Script:" );
        Console.WriteLine( script );

        Console.WriteLine( "\nCode:" );
        Console.WriteLine( code );
#endif
    }

}
