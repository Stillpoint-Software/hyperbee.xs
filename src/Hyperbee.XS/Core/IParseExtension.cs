using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS.Core;

public record ExtensionBinder(
    Parser<Expression> ExpressionParser,
    Deferred<Expression> StatementParser
);

[Flags]
public enum ExtensionType
{
    None = 0,
    Directive = 1,
    Literal = 2,
    Expression = 4,
    Terminated = 8,
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    string Key { get; }
    Parser<Expression> CreateParser( ExtensionBinder binder );
}
