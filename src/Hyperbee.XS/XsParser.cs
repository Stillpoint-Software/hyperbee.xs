using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Hyperbee.XS.Core.Parsers.XsParsers;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    private readonly Parser<Expression> _xs;
    private readonly XsConfig _config;

    public XsParser()
        : this( default )
    {
    }

    public XsParser( XsConfig config )
    {
        _config = config ?? new XsConfig();
        _xs = CreateParser( _config );
    }

    // Parse

    public Expression Parse( string script, XsDebugger debugger = null, ParseScope scope = null )
    {
        var scanner = new Scanner( script );
        var context = new XsContext( _config, debugger, scanner, scope ) { WhiteSpaceParser = WhitespaceOrNewLineOrComment() };

        try
        {
            return _xs.Parse( context );
        }
        catch ( ParseException ex )
        {
            throw new SyntaxException( ex.Message, context.Scanner.Cursor );
        }
    }

    // Parsers

    private static Parser<Expression> CreateParser( XsConfig config )
    {
        var statement = Deferred<Expression>();

        // Expressions

        var expression = ExpressionParser( statement, config );
        var expressionStatement = expression.WithTermination();

        expressionStatement = expressionStatement.Debuggable();

        // Directives

        var directives = KeywordLookup<Expression>( "keyword-directives" )
            .Add( UsingDirectiveParser() )
            .Add( config.Extensions.Statements( ExtensionType.Directive, expression, statement ) );

        // Compose Statements

        var terminatedStatements = KeywordLookup<Expression>( "keyword-terminated" )
            .Add(
                BreakParser(),
                ContinueParser(),
                GotoParser(),
                ReturnParser( expression ),
                ThrowParser( expression )
            ).Add(
                config.Extensions.Statements( ExtensionType.Terminated, expression, statement )
            );

        statement.Parser = OneOf(
            terminatedStatements,
            expressionStatement,
            LabelParser()
        ).Named( "statement" );

        // Create the final parser

        var xs = ZeroOrMany( directives )
            .And( ZeroOrMany( statement ) );

        return SynthesizeMethod( xs );
    }

    private static Parser<Expression> ExpressionParser( Deferred<Expression> statement, XsConfig config )
    {
        var expression = Deferred<Expression>();

        // Literals

        var literal = LiteralParser( config, expression );

        // Identifiers

        var variable = Variable();
        var typeConstant = TypeConstant();

        var identifier = OneOf(
            variable,
            typeConstant
        ).Named( "identifier" );

        // Grouped

        var groupedExpression = Between(
            Terms.Char( '(' ),
            expression,
            Terms.Char( ')' )
        ).Named( "group" );

        // Block

        var blockExpression = BlockParser( statement );

        // Expression statements

        var complexExpression = KeywordLookup<Expression>( "keyword-expression" )
            .Add(
                DefaultParser( typeConstant ),
                DeclarationParser( expression ),
                NewParser( expression ),
                ConditionalParser( expression, statement ),
                LoopParser( blockExpression ),
                TryCatchParser( statement ),
                SwitchParser( expression, statement )
            )
            .Add(
                config.Extensions.Statements( ExtensionType.Expression, expression, statement )
            ).RequireTermination( false );

        // Primary Expressions

        var lambdaExpression = LambdaParser( typeConstant, expression );

        var primaryExpression = OneOf(
            variable,
            literal,
            identifier,
            groupedExpression,
            blockExpression,
            complexExpression,
            lambdaExpression
        )
        .LeftAssociative( // accessors
            left => MemberAccessParser( left, expression ),
            left => LambdaInvokeParser( left, expression ),
            left => IndexerAccessParser( left, expression )
        )
        .LeftAssociative( // casting
            (Terms.Text( "as?" ), ( left, right ) => TypeAs( left, CastToType( right, nullable: true ) )),
            (Terms.Text( "as" ), ( left, right ) => Convert( left, CastToType( right ) )),
            (Terms.Text( "is" ), ( left, right ) => TypeIs( left, CastToType( right ) ))
        )
        .Named( "primary" );

        // Prefix and Postfix Expressions

        var prefixExpression = OneOf(
                Terms.Text( "++" ),
                Terms.Text( "--" )
            )
            .And( variable )
            .Then<Expression>( static parts =>
            {
                var (op, variable) = parts;

                return op switch
                {
                    "++" => PreIncrementAssign( variable ),
                    "--" => PreDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported prefix operator: {op}." )
                };
            } ).Named( "prefix" );

        var postfixExpression = variable
            .And(
                OneOf(
                    Terms.Text( "++" ),
                    Terms.Text( "--" )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (variable, op) = parts;

                return op switch
                {
                    "++" => PostIncrementAssign( variable ),
                    "--" => PostDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported postfix operator: {op}." )
                };
            } )
            .RequireTermination( require: true )
            .Named( "postfix" );

        // Unary Expressions

        var unaryExpression = OneOf(
            prefixExpression,
            postfixExpression,
            primaryExpression
        ).Unary(
            (Terms.Text( "?" ), IsTrue),
            (Terms.Text( "!?" ), IsFalse),
            (Terms.Text( "!" ), Not),
            (Terms.Text( "-" ), Negate),
            (Terms.Text( "~" ), OnesComplement)
        ).Named( "unary" );

        // Binary Expressions

        return expression.Parser = unaryExpression
            .RightAssociative(
                (Terms.Text( "**" ), SafePower)
            )
            .LeftAssociative(
                (Terms.Text( "*" ), Multiply),
                (Terms.Text( "/" ), Divide),
                (Terms.Text( "%" ), Modulo)
            )
            .LeftAssociative( // operator
                (Terms.Text( "+" ), Add),       // peek and use increment
                (Terms.Text( "-" ), Subtract),  // peek and use decrement
                (Terms.Text( "==" ), Equal),
                (Terms.Text( "!=" ), NotEqual),
                (Terms.Text( "<" ), LessThan),
                (Terms.Text( ">" ), GreaterThan),
                (Terms.Text( "<=" ), LessThanOrEqual),
                (Terms.Text( ">=" ), GreaterThanOrEqual),
                (Terms.Text( "&&" ), AndAlso),
                (Terms.Text( "||" ), OrElse),
                (Terms.Text( "??" ), Coalesce),

                // bitwise
                (Terms.Text( "^" ), ExclusiveOr),
                (Terms.Text( "&" ), And),
                (Terms.Text( "|" ), Or),
                (Terms.Text( ">>" ), LeftShift),
                (Terms.Text( "<<" ), RightShift)
            )
            .RightAssociative( // assignment
                (Terms.Text( "=" ), Assign),
                (Terms.Text( "+=" ), AddAssign),
                (Terms.Text( "-=" ), SubtractAssign),
                (Terms.Text( "*=" ), MultiplyAssign),
                (Terms.Text( "/=" ), DivideAssign),
                (Terms.Text( "%=" ), ModuloAssign),
                (Terms.Text( "**=" ), SafePowerAssign),
                (Terms.Text( "??=" ), CoalesceAssign),

                // bitwise
                (Terms.Text( "^=" ), ExclusiveOrAssign),
                (Terms.Text( "&=" ), AndAssign),
                (Terms.Text( "|=" ), OrAssign),
                (Terms.Text( ">>=" ), LeftShiftAssign),
                (Terms.Text( "<<=" ), RightShiftAssign)
            )
            .Named( "expression" );
    }

    // Helper parsers

    private static Parser<IReadOnlyList<Expression>> ArgsParser( Parser<Expression> expression )
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), expression ), [] );
    }

    private static Parser<IReadOnlyList<Type>> TypeArgsParser()
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), TypeRuntime() ), [] );
    }

    private static KeywordParserPair<Expression> UsingDirectiveParser()
    {
        return new(
            "using",
            Terms.NamespaceIdentifier().ElseInvalidIdentifier()
                .AndSkip( Terms.Char( ';' ) )
                .Then( ( ctx, parts ) =>
                {
                    var ns = parts.ToString();

                    if ( ctx is XsContext xsContext )
                        xsContext.Namespaces.Add( ns );

                    return new DirectiveExpression( $"using {ns}" ) as Expression;
                } )
        );
    }

    private static Parser<Expression> SynthesizeMethod( Sequence<IReadOnlyList<Expression>, IReadOnlyList<Expression>> parser )
    {
        return Bounded(
            static ctx =>
            {
                if ( ctx is not XsContext xsContext || xsContext.InitialScope )
                    ctx.EnterScope( FrameType.Method );
            },
            parser.Then( (ctx, parts ) => {
                var (directives, body) = parts;
                return ConvertToSingleExpression ( ctx, [..directives, ..body] );
            } ),
            static ctx =>
            {
                if ( ctx is not XsContext xsContext || xsContext.InitialScope )
                    ctx.ExitScope();

                ThrowIfNotEof( ctx );
            }
        );

        static void ThrowIfNotEof( ParseContext ctx )
        {
            var cursor = ctx.Scanner.Cursor;
            ctx.SkipWhiteSpace();

            if ( cursor.Eof == false )
                throw new SyntaxException( "Syntax Error. Failure parsing script.", cursor );
        }
    }

    // Helper methods

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Expression ConvertToSingleExpression( ParseContext context, IReadOnlyList<Expression> expressions )
    {
        if ( expressions == null || expressions.Count == 0 )
            return Default( typeof( void ) );

        var scope = context.Scope();

        var finalType = expressions[^1].Type;
        var returnLabel = scope.Frame.ReturnLabel;
        var locals = scope.Variables.EnumerateValues( Collections.KeyScope.Current ).ToArray();

        if ( expressions.Count == 1 && locals.Length == 0 )
            return expressions[0];

        if ( returnLabel == null )
            return Block( locals, expressions );

        if ( returnLabel.Type != finalType )
            throw new InvalidOperationException( $"Mismatched return types: Expected {returnLabel.Type}, found {finalType}." );

        return Block( locals, expressions.Concat( [Label( returnLabel, Default( returnLabel.Type ) )] ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Type TypeOf( Expression expression )
    {
        ArgumentNullException.ThrowIfNull( expression, nameof( expression ) );

        return expression switch
        {
            ConstantExpression ce => ce.Value as Type ?? ce.Type,
            Expression => expression.Type
        };
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Type CastToType( Expression expression, bool nullable = false )
    {
        if ( expression is not ConstantExpression ce || ce.Value is not Type type )
            throw new InvalidOperationException( "The right-side of a cast operator requires a Type." );

        if ( nullable && type.IsValueType )
            return typeof( Nullable<> ).MakeGenericType( type );

        return type;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static BinaryExpression CoalesceAssign( Expression left, Expression right )
    {
        return Assign( left, Coalesce( left, right ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static BinaryExpression SafePowerAssign( Expression left, Expression right )
    {
        return Assign( left, SafePower( left, right ) );
    }

    private static Expression SafePower( Expression left, Expression right )
    {
        // we have to do some type conversion here because the Power
        // method only accepts double, and we want to support all numeric
        // types while preserving the type of the left-hand side variable.

        var leftAsDouble = CastTo( left, typeof( double ) );
        var rightAsDouble = CastTo( right, typeof( double ) );
        var powerExpression = Power( leftAsDouble, rightAsDouble );

        return left.Type == typeof( double )
            ? powerExpression
            : Convert( powerExpression, left.Type );

        static Expression CastTo( Expression value, Type type )
        {
            return value.Type == type ? value : Convert( value, type );
        }
    }
}
