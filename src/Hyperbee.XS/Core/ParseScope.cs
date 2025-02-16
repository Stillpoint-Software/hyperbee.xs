using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Core;

public class ParseScope
{
    private readonly Stack<Frame> _frames = new();

    public LinkedDictionary<string, ParameterExpression> Variables = new();
    public Frame Frame => _frames.Peek();

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public virtual void EnterScope( FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        var parent = _frames.Count > 0 ? _frames.Peek() : null;
        var frame = new Frame( frameType, parent, breakLabel, continueLabel );

        _frames.Push( frame );
        Variables.Push();
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public virtual void ExitScope()
    {
        _frames.Pop();
        Variables.Pop();
    }
}

public enum FrameType
{
    Method,
    Statement,
    Block
}

public class Frame
{
    public FrameType FrameType { get; }
    public Frame Parent { get; }

    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }
    public LabelTarget ReturnLabel { get; private set; }

    public Dictionary<string, LabelTarget> Labels { get; } = new();

    public Frame( FrameType frameType, Frame parent = null, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        FrameType = frameType;
        Parent = parent;
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }

    public LabelTarget ResolveBreakLabel()
    {
        var currentFrame = GetEnclosingFrame( FrameType.Statement );

        return currentFrame.BreakLabel;
    }

    public LabelTarget ResolveContinueLabel()
    {
        var currentFrame = GetEnclosingFrame( FrameType.Statement );

        return currentFrame.ContinueLabel;
    }

    public LabelTarget GetOrCreateLabel( string labelName )
    {
        var currentFrame = GetEnclosingFrame( FrameType.Method );

        if ( currentFrame.Labels.TryGetValue( labelName, out var label ) )
            return label;

        currentFrame.Labels[labelName] = label = Label( labelName );
        return label;
    }

    public LabelTarget GetOrCreateReturnLabel( Type returnType )
    {
        var currentFrame = GetEnclosingFrame( FrameType.Method );

        currentFrame.ReturnLabel ??= Label( returnType, "ReturnLabel" );

        if ( currentFrame.ReturnLabel.Type != returnType )
            throw new InvalidOperationException( $"Mismatched return types: Expected {currentFrame.ReturnLabel.Type}, found {returnType}." );

        return currentFrame.ReturnLabel;
    }

    private Frame GetEnclosingFrame( FrameType frameType )
    {
        var currentFrame = this;
        while ( currentFrame != null )
        {
            if ( currentFrame.FrameType == frameType )
                return currentFrame;

            currentFrame = currentFrame.Parent;
        }

        throw new InvalidOperationException( $"No enclosing {frameType} frame found." );
    }
}
