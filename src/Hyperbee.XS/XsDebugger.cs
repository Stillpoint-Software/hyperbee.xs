using System.Collections.ObjectModel;
using Hyperbee.XS.Core;

namespace Hyperbee.XS;

public enum BreakMode
{
    None,
    Call, // debug()
    Statements
}

public struct DebugBreak
{
    public XsDebugger Debugger { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public string SourceLine { get; init; }
    public ReadOnlyDictionary<string, object> Variables { get; init; }
}

public class XsDebugger
{
    public List<Breakpoint> Breakpoints { get; set; }
    public DebuggerCallback Callback { private get; init; }

    public BreakMode BreakMode { get; set; } = BreakMode.Call;

    public bool TryBreak( int line, int column, Dictionary<string, object> variables, string sourceLine )
    {
        if ( BreakMode == BreakMode.None || Callback == null )
            return false;

        if ( Breakpoints != null && !AnyBreakpoint( line, column ) )
            return false;

        var debugBreak = new DebugBreak
        {
            Debugger = this,
            Line = line,
            Column = column,
            Variables = new ReadOnlyDictionary<string, object>( variables ),
            SourceLine = sourceLine
        };

        Callback( debugBreak );
        return true;
    }

    private bool AnyBreakpoint( int line, int column )
    {
        return Breakpoints.Any( x => x.Line == line && (x.Columns == null || x.Columns.Contain( column )) );
    }

    public record Breakpoint( int Line, ColumnRange Columns = null );

    public record ColumnRange( int Start, int End )
    {
        internal bool Contain( int column ) => column >= Start && column <= End;
    }

    public delegate void DebuggerCallback( DebugBreak debugBreak );
}

