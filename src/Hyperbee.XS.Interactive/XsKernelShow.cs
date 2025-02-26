using Hyperbee.XS.Core.Writer;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.Interactive;

public class XsKernelShow : XsBaseKernel,
    IKernelCommandHandler<SubmitCode>
{
    public XsKernelShow() : base( "xs-show" )
    {
        KernelInfo.LanguageName = "XS (show)";
        KernelInfo.Description = """
                                 Show Expression Script as a C# expression tree
                                 """;
    }

    public Task HandleAsync( SubmitCode command, KernelInvocationContext context )
    {
        try
        {
            Parser.Value
                .Parse( command.Code )
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
