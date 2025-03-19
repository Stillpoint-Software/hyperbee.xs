using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.Interactive;

public class XsKernelExtension : IKernelExtension, IStaticContentSource
{
    public string Name => "Expression Script (XS)";

    public async Task OnLoadAsync( Kernel kernel )
    {
        if ( kernel is not CompositeKernel compositeKernel )
            throw new InvalidOperationException( "The XS kernel could not be loaded." );

        compositeKernel.Add(
            new XsKernel()
                .UseWho()
                .UseValueSharing()
        );

        compositeKernel.Add( new XsKernelShow() );

        var message = new HtmlString(
            """
            <details>
                <summary>Expression Script (XS) Kernel support with !#xs and !#xs-show commands.</summary>
                <p>This extension adds a new kernel that can execute Expression Script (XS).</p>
                <p>Use <code>!#xs</code> to compile and run Expression Script code.</p>
                <p>Use <code>!#xs-show</code> to show Expression Script as a C# expression tree.</p>
                <p> Supported Commands:</p>
                <ul>
                    <li><code>!#import</code> to import extensions from other kernels or NuGet packages</li>
                    <li><code>!#share</code> to share values between kernels.</li>
                    <li><code>!#whos</code> to list all shared values.</li>
                </ul>
            </details>
            """ );

        var formattedValue = new FormattedValue(
            HtmlFormatter.MimeType,
            message.ToDisplayString( HtmlFormatter.MimeType ) );

        await compositeKernel.SendAsync( new DisplayValue( formattedValue, Guid.NewGuid().ToString() ) );
    }
}
