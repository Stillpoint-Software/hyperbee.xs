﻿using System.Linq.Expressions;
using Hyperbee.XS.Core.Writer;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Hyperbee.XS.Tests;

[TestClass]
public class ExpressionStringTests
{
    private static readonly ScriptOptions ScriptOptions = ScriptOptions.Default.WithReferences(
        [
            "System",
            "System.Linq.Expressions",
            "Hyperbee.XS.Tests"
        ]
    );

    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

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
                    goto there;
                case 2:
                    result = 2;
                    goto there;
                default:
                    result = -1;
                    goto there;
            }
            there:
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

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_IsTrueAndIsFalse()
    {
        var script = """
            var x = ?true;
            var y = !?false;
            x && y;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<bool>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_TypeAs()
    {
        var script = """
            var obj = "test";
            var result = obj as? string;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_Convert()
    {
        var script = """
            var obj = "test";
            var result = obj as string;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_TypeIs()
    {
        var script = """
            var obj = "test";
            var result = (obj is string);
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<bool>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_CoalesceAssign()
    {
        var script = """
            var x = "hello";
            var result = default(string);
            result ??= x;
            result;
            """;

        var expression = Xs.Parse( script );
        var code = expression.ToExpressionString();

        WriteResult( script, code );

        var lambda = Expression.Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        await AssertScriptValue( code, result );
    }

    [TestMethod]
    public async Task ToExpressionString_ShouldCreate_PowerAssign()
    {
        var script = """
            var x = 5;
            var result = 2;
            result **= x;
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
    public async Task ToExpressionString_ShouldCreate_MathOperations()
    {
        var script = """
            var x = 5;
            var y = 10;
            var result = x + y * 2 - (x / y);
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
    public async Task ToExpressionString_ShouldCreate_AssignmentOperations()
    {
        var script = """
            var x = 5;
            x += 10;
            x -= 3;
            x *= 2;
            x /= 4;
            x;
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
        var typeName = typeof( T ).Name;

        var scriptResult = await CSharpScript.EvaluateAsync<T>(
            code +
            $"var lambda = Expression.Lambda<Func<{typeName}>>( expression );" +
            "var compiled = lambda.Compile();" +
            "return compiled();", ScriptOptions );

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
