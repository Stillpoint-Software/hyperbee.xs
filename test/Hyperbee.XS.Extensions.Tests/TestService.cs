using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hyperbee.XS.Extensions.Tests;

public interface ITestService
{
    string DoSomething();
}

public class TestService : ITestService
{
    public TestService() { }
    public TestService( string extra ) => Extra = extra;
    public string Extra { get; set; }

    public string DoSomething() => "Hello, World!" + Extra;
}

public static class ServiceProvider
{
    public const string Key = "Hello";
    public const string Value = "Hello, World!";

    public static IServiceProvider GetServiceProvider()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices( ( _, services ) =>
            {
                services.AddSingleton<ITestService, TestService>();
                services.AddKeyedSingleton<ITestService>( "TestKey", ( _, _ ) => new TestService( " And Universe!" ) );
            } )
            .ConfigureAppConfiguration( ( _, config ) =>
            {
                config.AddInMemoryCollection( new Dictionary<string, string>
                {
                    {Key, Value}
                } );
            } )
            .Build();

        return host.Services;
    }
}
