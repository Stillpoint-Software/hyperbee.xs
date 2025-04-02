using Hyperbee.Expressions;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class InjectExtensionsTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithInject()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            using Hyperbee.XS.Extensions.Tests;
            inject<ITestService>();
            """ );

        var lambda = Lambda<Func<ITestService>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( "Hello, World!", result.DoSomething() );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithInjectAndRun()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            using Hyperbee.XS.Extensions.Tests;
            var service = inject<ITestService>();
            service.DoSomething();
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( "Hello, World!", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithKeyedInject()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            using Hyperbee.XS.Extensions.Tests;
            inject<ITestService>("TestKey");
            """ );

        var lambda = Lambda<Func<ITestService>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( "Hello, World! And Universe!", result.DoSomething() );
    }

}
