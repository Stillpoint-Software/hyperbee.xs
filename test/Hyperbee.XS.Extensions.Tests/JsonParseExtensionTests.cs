using System.Text.Json;
using Hyperbee.Json.Extensions;
using Hyperbee.Xs.Extensions.Lab;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class JsonParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( GetXsConfig() );

    private static XsConfig GetXsConfig()
    {
        var config = TestInitializer.XsConfig;
        config.Extensions.Add( new JsonParseExtension() );
        return config;
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithJsonString()
    {
        var expression = Xs.Parse(
            """"
            json """
            { 
                "First": "Joe", 
                "Last": "Jones" 
            }
            """;
            """" );

        var lambda = Lambda<Func<JsonElement>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "Jones", result.Select( "$.Last" ).First().GetString() );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithExpression()
    {
        var expression = Xs.Parse(
            """"
            var s = """
            { 
                "First": "Joe", 
                "Last": "Jones" 
            }
            """;
            
            var x = json s;

            x;
            """" );

        var lambda = Lambda<Func<JsonElement>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "Jones", result.Select( "$.Last" ).First().GetString() );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithExpressionStream()
    {
        var expression = Xs.Parse(
            """"
            using System.IO;
            
            var stream = new MemoryStream();
            var writer = new StreamWriter( stream );
            writer.Write( 
            """
            { 
                "First": "Joe", 
                "Last": "Jones" 
            }
            """ );
            writer.Flush();
            stream.Position = 0L;

            (json<Person> stream as Stream).Last;
            """" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "Jones", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithType()
    {
        var expression = Xs.Parse(
            """"
            ( json<Person> """
            { 
                "First": "Joe", 
                "Last": "Jones" 
            }
            """ ).Last;
            """" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "Jones", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithJsonPath()
    {
        var expression = Xs.Parse(
            """"
            var x = json """
            { 
                "First": "Joe", 
                "Last": "Jones" 
            }
            """::'$.Last';

            x.First().GetString();
            """" );

        var lambda = Lambda<Func<string>>( expression );

        var function = lambda.Compile();
        var result = function();

        Assert.AreEqual( "Jones", result );
    }

}

public record Person( string First, string Last )
{
    public override string ToString() => $"{Last}, {First}";
}
