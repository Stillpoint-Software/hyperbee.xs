using System.Linq.Expressions;
using Hyperbee.XS.Core.Writer;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class DebugParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Debug_AllLanguageFeatures()
    {
        var script =
        """
        var results = new List<int>(5);

        debug();

        var c = 0;
        if (1 + 1 == 2)
        {
            c = if (true) { 42; } else { 0; };
        }
        else
        {
            c = 1;
        }
        results.Add(c);
        
        var s = 3;
        switch (s)
        {
            case 1: s = 1; goto there;
            case 2: s = 2; goto there;
            default: s = 42; goto there;
        }
        there:
        results.Add(s);
        
        var t = 1;
        try
        {
            throw new ArgumentException();
        }
        catch (InvalidOperationException)
        {
            t = 0;
        }
        catch (ArgumentException)
        {
            t += 40;
        }
        finally
        {
            t += 1;
        }
        results.Add(t);

        var l = 0;
        for ( var i = 0; i < 42; i++ )
        {
            l++;
        }
        results.Add(l);

        var calc = (int a, int b) => a * b;
        results.Add( calc(6, 7) );

        debug();
        
        results;
        """;

        var debugger = new XsDebugger()
        {
            BreakMode = BreakMode.Statements,
            Handler = d =>
            {
                Console.WriteLine( $"Line: {d.Line}, Column: {d.Column}, Variables: {d.Variables}, Text: {d.SourceLine}" );
            }
        };

        var expression = Xs.Parse( script, debugger );

        var code = expression.ToExpressionString();

        Console.WriteLine( "Script:" );
        Console.WriteLine( script );

        Console.WriteLine( "\nCode:" );
        Console.WriteLine( code );

        var lambda = Expression.Lambda<Func<List<int>>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        // Assertions for each feature
        Assert.AreEqual( 5, result.Count ); // total number of features

        Assert.AreEqual( 42, result[0] ); // If-Else logic
        Assert.AreEqual( 42, result[1] ); // Switch-Case logic
        Assert.AreEqual( 42, result[2] ); // Try-Catch-Finally
        Assert.AreEqual( 42, result[3] ); // Loop 
        Assert.AreEqual( 42, result[4] ); // Lambda calculation (6 * 7)
    }
}
