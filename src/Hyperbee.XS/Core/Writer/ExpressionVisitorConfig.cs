namespace Hyperbee.XS.Core.Writer;

public record ExpressionVisitorConfig(
    string Prefix = "Expression.",
    string Indentation = "  ",
    string Variable = "expression",
    params IExpressionWriter[] Writers );
