using Microsoft.DotNet.Interactive.Commands;

namespace Hyperbee.Xs.Interactive.Extensions;

public class ExtensionCommand( string extension ) : KernelCommand
{
    public string Extension { get; set; } = extension;
}
