using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Core.Parsers;

internal class TypeConstantParser : Parser<Expression>
{
    private readonly bool _backtrack;

    public TypeConstantParser( bool backtrack )
    {
        _backtrack = backtrack;
        Name = "TypeConstant";
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        context.EnterParser( this );

        var typeResult = new ParseResult<Type>();

        if ( XsParsers.TypeRuntime( _backtrack ).Parse( context, ref typeResult ) )
        {
            var resolvedType = typeResult.Value;
            result.Set( typeResult.Start, typeResult.End, Expression.Constant( resolvedType ) );
            context.ExitParser( this );
            return true;
        }

        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static Parser<Expression> TypeConstant( bool backtrack = true )
    {
        return new TypeConstantParser( backtrack );
    }
}
