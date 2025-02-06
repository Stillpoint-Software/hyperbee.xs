namespace Hyperbee.XS.Core.Writer;

public record XsVisitorConfig(
    string Indentation = "  ",
    params IXsWriter[] Writers );
