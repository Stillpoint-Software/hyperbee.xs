using System.Linq.Expressions;
using Hyperbee.Collections;
using Hyperbee.XS.Core;

namespace Hyperbee.XS.Interpreter;

public class InterpretScope : ParseScope
{
    public LinkedDictionary<ParameterExpression, object> Values { get; } = new();

    public override void EnterScope( FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        base.EnterScope( frameType, breakLabel, continueLabel );
        Values.Push();
    }

    public override void ExitScope()
    {
        Values.Pop();
        base.ExitScope();
    }
}
