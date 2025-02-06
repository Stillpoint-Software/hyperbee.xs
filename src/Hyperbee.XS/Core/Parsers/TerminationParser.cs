using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Core.Parsers;

public static partial class XsParsers
{
    public static Parser<T> RequireTermination<T>( this Parser<T> parser, bool require )
    {
        return parser.When( ( context, _ ) =>
        {
            var xsContext = (XsContext) context;
            xsContext.RequireTermination = require;
            return true;
        } );
    }

    public static Parser<T> WithTermination<T>( this Parser<T> parser )
    {
        return parser.AndSkipIf(
            ( ctx, _ ) => ((XsContext) ctx).RequireTermination,
            OneOrMany( Terms.Char( ';' ) ),
            ZeroOrMany( Terms.Char( ';' ) ).RequireTermination( true )
        ).Named( "Termination" );
    }
}
