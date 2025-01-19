﻿using System.Linq.Expressions;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS;

public partial class XsParser
{
    private static Parser<Expression> LiteralParser( XsConfig config, Deferred<Expression> expression )
    {
        var integerLiteral = Terms.Number<int>( NumberOptions.AllowLeadingSign )
            .AndSkip( ZeroOrOne( Terms.Text( "N", caseInsensitive: true ) ) )
            .Then<Expression>( static value => Constant( value ) );

        var longLiteral = Terms.Number<long>( NumberOptions.AllowLeadingSign )
            .AndSkip( Terms.Text( "L", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var floatLiteral = Terms.Number<float>( NumberOptions.Float )
            .AndSkip( Terms.Text( "F", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var doubleLiteral = Terms.Number<double>( NumberOptions.Float )
            .AndSkip( Terms.Text( "D", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var booleanLiteral = Terms.Text( "true" ).Or( Terms.Text( "false" ) )
            .Then<Expression>( static value => Constant( bool.Parse( value ) ) );

        var characterLiteral = Terms.CharQuoted( StringLiteralQuotes.Single )
            .Then<Expression>( static value => Constant( value ) );

        var stringLiteral = Terms.String( StringLiteralQuotes.Double )
            .Then<Expression>( static value => Constant( value.ToString() ) );

        var nullLiteral = Terms.Text( "null" ).Then<Expression>( static _ => Constant( null ) );

        var literal = OneOf(
            longLiteral,
            doubleLiteral,
            floatLiteral,
            integerLiteral,
            characterLiteral,
            stringLiteral,
            booleanLiteral,
            nullLiteral
        ).Or(
            OneOf(
                LiteralExtensions( config, expression )
            )
        ).Named( "literal" );

        return literal;
    }
}
