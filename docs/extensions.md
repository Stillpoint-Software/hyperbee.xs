---
layout: default
title: Extensions
nav_order: 5
---
# Extensions

XS supports extensibility by allowing developers to integrate **custom expressions** and syntax directly into the parser. 

## **Steps to Extend XS**

1. Create a custom Expression.
2. Create a parser Extension that defines the syntax for the custom Expression.
3. Register the Extension with the parser.
4. Use the Extension in your scripts .

## **Example: A Custom `RepeatExpression`**

The `RepeatExpression` executes a block of code a specified number of times, similar to a `for` loop.

### **1. Create a Custom Expression**

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

### **2. Create a Parser Extension**

An extension class includes logic to parse the `repeat` syntax and instantiate the custom `RepeatExpression`. 

**Repeat Parser Extension:**

```csharp
public class RepeatParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "repeat";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
       var (expression, statement) = binder;

        return Between(
            Terms.Char('('),
            expression,
            Terms.Char(')')
        )
        .And( 
             Between(
                Terms.Char('{'),
                statement,
                Terms.Char('}')
            )
        )
        .Then<Expression>( static parts =>
        {
            var (countExpression, body) = parts;
            return new RepeatExpression(countExpression, body);
        });
    }
}
```

### **3. Register the Extension**

```csharp
var config = new XsConfig { Extensions = [ new RepeatParseExtension() ] };
var parser = new XsParser( config );
```

### **4. Use the Extension**

You can now use the `repeat` keyword in your scripts:

```plaintext
var x = 0;
repeat (5) {
    x++;
}
```

## **Summary**

By using extensions, developers can:

* Introduce new syntax into XS.
* Implement custom expressions like `RepeatExpression`.
* Keep the parser modular and extensible.
* Integrate the custom expressions into the generated expression tree pipeline.
