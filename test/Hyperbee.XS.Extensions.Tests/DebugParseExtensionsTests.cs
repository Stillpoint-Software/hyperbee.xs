using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Xs.Extensions;
using Hyperbee.XS.System.Writer;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class DebugParseExtensionTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig
        {
            References = [Assembly.GetExecutingAssembly()],
            Extensions = XsExtensions.Extensions()
        }
    );

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

        results;
        """;

        var debugInfo = new XsDebugInfo()
        {
            Debugger = ( l, c, v, m ) =>
            {
                Console.WriteLine( $"Line: {l}, Column: {c}, Variables: {v}, Message: {m}" );
            }
        };

        var expression = Xs.Parse( script, debugInfo );

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
