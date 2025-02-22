﻿using System.Linq.Expressions;

namespace Hyperbee.XS.Interpreter;

public sealed class GotoNavigationVisitor : ExpressionVisitor
{
    private readonly Stack<Expression> _pathStack = new();
    private readonly Dictionary<LabelTarget, List<Expression>> _labelPaths = new();
    private readonly Dictionary<GotoExpression, List<Expression>> _gotoPaths = new();
    private readonly Dictionary<GotoExpression, GotoNavigation> _gotoNavigation = new();

    public Dictionary<GotoExpression, GotoNavigation> Analyze(Expression root)
    {
        _gotoPaths.Clear();
        _labelPaths.Clear();
        _gotoNavigation.Clear();

        Visit(root);
        ResolveNavigationPaths();

        return _gotoNavigation;
    }

    public override Expression Visit(Expression node)
    {
        if (node == null)
            return null;

        _pathStack.Push(node);
        var result = base.Visit(node);
        _pathStack.Pop();

        return result;
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        _labelPaths[node.Target] = new List<Expression>(_pathStack.Reverse());
        return base.VisitLabel(node);
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        _gotoPaths[node] = new List<Expression>(_pathStack.Reverse());
        return base.VisitGoto(node);
    }

    private void ResolveNavigationPaths()
    {
        foreach (var (gotoExpr, gotoPath) in _gotoPaths)
        {
            if (!_labelPaths.TryGetValue(gotoExpr.Target, out var labelPath))
            {
                throw new InvalidOperationException($"Label target {gotoExpr.Target.Name} not found.");
            }

            _gotoNavigation[gotoExpr] = CreateNavigationExpression(gotoPath, labelPath, gotoExpr.Target);
        }
    }

    private static GotoNavigation CreateNavigationExpression( List<Expression> gotoPath, List<Expression> labelPath, LabelTarget targetLabel )
    {
        var minLength = Math.Min( gotoPath.Count, labelPath.Count );
        var ancestorIndex = 0;

        while( true )
        {
            if ( gotoPath[ancestorIndex] != labelPath[ancestorIndex] )
                break;

            if ( ++ancestorIndex == minLength )
                throw new InvalidOperationException( "Could not determine a common ancestor." );
        }

        var commonAncestorExpr = labelPath[ancestorIndex - 1];
        var steps = labelPath.Skip( ancestorIndex ).ToList();

        return new GotoNavigation( commonAncestorExpr, steps, targetLabel, false );
    }
}

public sealed class GotoNavigation
{
    public Expression CommonAncestor { get; }
    public List<Expression> Steps { get; }
    public LabelTarget TargetLabel { get; }
    public bool IsReturn { get; }
    private int _currentStepIndex;

    public GotoNavigation( Expression commonAncestor, List<Expression> steps, LabelTarget targetLabel, bool isReturn )
    {
        CommonAncestor = commonAncestor;
        Steps = steps;
        TargetLabel = targetLabel;
        IsReturn = isReturn;
        _currentStepIndex = 0;
    }

    public Expression Current => _currentStepIndex < Steps.Count ? Steps[_currentStepIndex] : null;
    public bool IsEmpty => _currentStepIndex >= Steps.Count;
    public void Reset() => _currentStepIndex = 0;

    public Expression GetNextStep()
    {
        if ( _currentStepIndex >= Steps.Count )
            throw new InvalidOperationException( "No more steps available." );

        return Steps[_currentStepIndex++];
    }
}

