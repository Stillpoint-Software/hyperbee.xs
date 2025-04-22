using System.Net;
using System.Text;
using System.Text.Json;
using Hyperbee.Json.Extensions;
using Hyperbee.Xs.Extensions.Lab;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class FetchParseExtensionTests
{
    public static XsParser Xs { get; set; } = new( GetXsConfig() );

    private static XsConfig GetXsConfig()
    {
        var config = TestInitializer.XsConfig;
        config.Extensions.Add( new FetchParseExtension() );
        config.Extensions.Add( new JsonParseExtension() );
        return config;
    }

    [TestMethod]
    public async Task Parse_ShouldSucceed_WithFetch()
    {
        var serviceProvider = GetServiceProvider();

        var expression = Xs.Parse(
            """
            fetch( "Test", "/api" )
            """ );

        var lambda = Lambda<Func<Task<HttpResponseMessage>>>( expression );

        var function = lambda.Compile( serviceProvider, preferInterpretation: false );
        var result = await function();

        Assert.IsNotNull( result );
        Assert.AreEqual( HttpStatusCode.OK, result.StatusCode );
    }

    [TestMethod]
    public async Task Parse_ShouldSucceed_WithFetchAndJsonBody()
    {
        var serviceProvider = GetServiceProvider();

        var expression = Xs.Parse(
            """
            async {
                var response = await fetch( "Test", "/api" );
                json response::'$.mockKey';
            }
            """ );

        var lambda = Lambda<Func<Task<IEnumerable<JsonElement>>>>( expression );

        var function = lambda.Compile( serviceProvider, preferInterpretation: false );
        var result = await function();

        Assert.AreEqual( "mockValue", result.Single().GetString() );
    }

    private static IServiceProvider GetServiceProvider( HttpMessageHandler messageHandler = null )
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices( ( _, services ) =>
            {
                services.AddSingleton( new JsonSerializerOptions() );

                // Replace HttpClient with a mock or fake implementation for testing
                services.AddHttpClient( "Test", ( client ) =>
                    {
                        client.BaseAddress = new Uri( "https://example.com" );
                    } )
                    .ConfigurePrimaryHttpMessageHandler( () => messageHandler ?? new MockHttpMessageHandler() );
            } )
            .Build();

        return host.Services;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler( HttpStatusCode statusCode = HttpStatusCode.OK )
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken )
        {
            return Task.FromResult( new HttpResponseMessage( _statusCode )
            {
                Content = new StringContent( "{\"mockKey\":\"mockValue\"}", Encoding.UTF8, "application/json" )
            } );
        }
    }

}

