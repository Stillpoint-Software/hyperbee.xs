using Hyperbee.XS.Core.Writer;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.Interactive;

public class XsKernelShow() : 
    XsBaseKernel( "xs-show" ),
    IKernelCommandHandler<SubmitCode>
{
    public Task HandleAsync( SubmitCode command, KernelInvocationContext context )
    {
        try
        { 
            var parser = new XsParser( Config );

            parser.Parse( command.Code )
                .ToExpressionString()
                .Display( PlainTextFormatter.MimeType );
        }
        catch ( Exception ex )
        {
            context.Fail( command, message: ex.Message );
        }

        return Task.CompletedTask;
    }
}
