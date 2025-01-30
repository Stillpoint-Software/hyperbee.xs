---
layout: default
title: Extensions
nav_order: 4
---
# Extensions

XS supports extensibility by allowing developers to define **custom expressions** and integrate them directly into the parser. 
This provides a clean and flexible way to extend the language syntax for specific use cases or additional functionality.

## **Steps to Add a Custom Expression**

1. Create a custom Expression
2. Create a parser Extension that defines the syntax for the custom Expression
3. Register the extension with the parser

## **Example: A Custom `RepeatExpression`**

The `RepeatExpression` executes a block of code a specified number of times, similar to a `for` loop.

### **1. Define a Custom Expression**

```csharp
public class RepeatExpression : Expression
{
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(void);

    public Expression Count { get; }
    public Expression Body { get; }

    public RepeatExpression(Expression count, Expression body)
    {
        Count = count;
        Body = body;
    }

    public override Expression Reduce()
    {
        var loopVariable = Expression.Parameter(typeof(int), "i");

        return Expression.Block(
            new[] { loopVariable },
            Expression.Assign(loopVariable, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(loopVariable, Count),
                    Expression.Block(Body, Expression.PostIncrementAssign(loopVariable)),
                    Expression.Break(Expression.Label())
                )
            )
        );
    }
}
```

### **2. Create the Parser Extension Plugin**

A plugin object includes logic to parse the `repeat` syntax and generate the custom `RepeatExpression`. The extension implements `IParserExtension`:

public interface IParseExtension
{
    ExtensionType Type { get; }
    string Key { get; }
    Parser<Expression> CreateParser( ExtensionBinder binder );
}


**Plugin Implementation:**

```csharp
public class RepeatParseExtension : IParseExtension
{
    public ExtensionsType => ExtensionType.Complex;
    public string Key => "repeat";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
       var (config, expression, assignable, statement) = binder;

        return Between(
            Terms.Char('('),
            expression,
            Terms.Char(')
        )
        .And( 
             Between(
                Terms.Char('{'),
                statement,
                Terms.Char('}')
            )
        ).Then( parts =>
        {
            var (countExpression, body) = parts;)
            return new RepeatExpression(countExpression, body);
        });
    }
}
```

### **3. Register the Custom Expression in the Parser**

```csharp
var config = new XsConfig { Extensions = [ new RepeatParseExtension() ] };
var parser = new XsParser( config );
```

### **4. Use the Custom Expression**

With the parser updated, you can now use the `repeat` keyword in your scripts:

```plaintext
var x = 0;
repeat (5) {
    x++;
}
```

## **Summary**

By using extensions, developers can:

1. Introduce new syntax into XS.
2. Implement custom expressions like `RepeatExpression`.
3. Keep the parser modular and extensible.
4. Integrate the custom expressions into the generated expression tree pipeline.

This approach ensures that XS remains flexible, extensible, and adaptable to domain-specific needs with minimal effort.
