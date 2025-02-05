namespace Hyperbee.XS.System.Writer;

public record XsVisitorConfig(
    string Indentation = "  ",
    params IXsWriter[] Writers );
