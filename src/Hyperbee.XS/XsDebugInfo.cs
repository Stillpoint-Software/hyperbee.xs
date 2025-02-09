namespace Hyperbee.XS;

public class XsDebugInfo //BF XsDebugger
{
    internal string Source { get; set; }
    public List<Breakpoint> Breakpoints { get; set; }
    public DebuggerCallback Debugger { get; set; } //BF Callback { private get; init; }

    //BF Add enum for debug mode
    /*
        public enum DebugMode
        {
            None,
            Call,   // debug()
            Step    // statements
        }

        public DebugMode Mode { get; set; } = DebugMode.Call;
    */

    internal void InvokeDebugger( int line, int column, Dictionary<string, object> variables, string sourceLine ) //BF Invoke
    {
        //BF if ( Mode == DebugMode.None ) return;

        if ( Breakpoints == null || Breakpoints.Any( bp => bp.Line == line && (bp.Columns == null || bp.Columns.Contain( column )) ) )
        {
            Debugger?.Invoke( line, column, variables, sourceLine );
        }
    }

    public record Breakpoint( int Line, ColumnRange Columns = null );

    public record ColumnRange( int Start, int End )
    {
        internal bool Contain( int column ) => column >= Start && column <= End;
    }

    //BF pass XsDebugger
    //   remove sourceLine
    //   add GetLine() to XsDebugger
    public delegate void DebuggerCallback( int line, int column, Dictionary<string, object> variables, string sourceLine );  
}

