namespace Hyperbee.XS.Core.Writer;

public record ExpressionVisitorConfig(
    string Prefix = "Expression.",
    string Indentation = "  ",
    string Variable = "expression",
    string[] Usings = null,
    params IExpressionWriter[] Writers );
