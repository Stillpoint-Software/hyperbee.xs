---
layout: default
title: Overview
nav_order: 1
---
# XS: A Lightweight, Extensible Scripting Language for Expression Trees :rocket:

### **What is XS?**

XS is a lightweight scripting language designed to simplify and enhance the use of C# expression trees.
It provides a familiar C#-like syntax while offering advanced extensibility, making it a compelling choice for developers
who want to use dynamic code in their applications.

XS lowers the barrier for developers who want to leverage expression trees, eliminating the complexity of manually constructing, 
extending, and navigating expression tree syntax.

---

### **Why Does XS Matter?**

#### **Expression Trees Are Complex**:

Microsoft highlights that "the more complicated the expression tree that you want to build, the more difficult the code
is to manage and to read." ([Microsoft Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/expression-trees-building))

C# expression trees are a powerful feature that represent code as data structures, enabling dynamic runtime evaluation
and transformations. However, creating and maintaining them manually can be complex and error-prone, especially for 
larger or nested constructs like `if-else`, `switch`, `loops`, or `exception handling`.

Developers typically avoid using expression trees for anything beyond trivial cases due to this complexity.

#### **XS's Contribution**:

XS abstracts this complexity, allowing developers to write scripts that directly translate to and from expression trees. 

---

### **Extensibility Beyond Native Expression Trees**:

XS is easy to extended to support new language features.

This is important because native C# expression trees lack support for modern language features such as `async/await`, 
`string interpolation`, `for/foreach` loops, and `using statements`. XS provides full support for these features through
extensions.

Consider this example of a `foreach` loop in XS. `foreach` is not natively supported in C# expression trees, but XS 
supports it through a custom `Expression` extension, that reduces to native `Expression`s.

  ```csharp
  var expression = XsParser.Parse(
      """
      var array = new int[] { 1,2,3 };
      var x = 0;
      
      foreach ( var item in array )
      {
          x = x + item;
      }

      async
      {
          await Task.Delay( 1000 );
      }
      
      x;
      """ );

  var lambda = Lambda<Func<int>>( expression );

  var compiled = lambda.Compile();
  var result = compiled();

  Assert.AreEqual( 6, result );
  ```

XS allows developers to create and execute high-level logic like this without requiring manual tree construction.

Extending XS is straightforward and modular. Developers can focus on the high-level behavior of their custom 
expressions without needing to handle low-level parsing or tree construction manually. 
  
---

### **Unified Syntax and Model**:

In XS, **everything is an expression**, including language extensions. This design makes the language highly 
composable and directly aligns with expression tree concepts, enabling features not always directly possible in C#
(like implicit returns).

Example: An `if`, `switch`, and `block` statements can return a values directly, allowing constructs like
`c = if (true) { 42; } else { 0; };`. This composability eliminates artificial boundaries between statements and 
expressions, resulting in a cleaner and more intuitive scripting model.

---

### **Performance and Flexibility**:

- Generates Expression Trees: XS generates standard expression trees, ensuring runtime performance is as good as-or 
better than-handwritten expression trees.

- Compiler Agnosticism: XS supports both 
[Expression.Compile()](https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression-1.compile) and
[FastExpressionCompiler (FEC)](https://github.com/dadhi/FastExpressionCompiler)), giving developers full control over 
how to execute scripts.

- Lightweight Parsing: XS is built on [Parlot](https://github.com/sebastienros/parlot), a high-performance parser 
combinator that outperforms alternatives like Sprache and Superpower in both speed and memory usage. XS avoids the 
bloat of Roslyn while still offering advanced language features, ensuring a lightweight and efficient runtime.

- No Overhead After Compilation: While parsing adds some overhead initially, once XS compiles a script into an 
expression tree, the compiled tree can be reused repeatedly without additional parsing or runtime costs.

- By integrating FEC, XS can compile expression trees directly into reloadable assemblies, providing significant 
performance improvements and enabling dynamic updates without application restarts.

---

### **How Does XS Compare to Roslyn?**  

**Different tools for different needs:** Roslyn is a full compiler designed for analyzing, transforming, and compiling 
complete C# programs. XS is a lightweight parsing engine optimized for expression tree generation and execution.  

#### **Why Use XS Instead of Roslyn for Runtime Execution?** 
- Enhances Expressions – XS simplifies expression tree creation and management, enabling developers to focus on code, not AST syntax. 
- Lower Overhead – XS generates and executes expression trees directly, avoiding Roslyn's full compilation pipeline and reducing startup costs.  
- Optimized for Dynamic Execution – Ideal for rule engines, embedded scripting, and DSLs, where Roslyn's full compilation step is unnecessary.  
- Expression Tree Native – XS treats code as data, enabling runtime introspection, transformation, and optimization before execution.  
- Extensible by Design – XS allows custom language constructs, control flow features, and operators, making it an ideal fit for domain-specific scripting.  

XS is **not a Roslyn replacement**—it serves a different purpose: **fast, lightweight, and embeddable runtime execution 
without full compiler overhead**. If you need to compile and analyze full C# programs, Roslyn is the right tool. If 
you need a small, efficient, and customizable scripting language for runtime execution, or you want to easily generate expression 
trees, XS is a solid choice.

---

### **Key Features of XS**

#### **Language Features**:

XS provides one-to-one support for all expression tree types, ensuring full alignment with the underlying runtime capabilities.

It supports:

- Control Flow: Try-catch, conditionals (`if`, `switch`), and looping constructs (`for`, `while`, `foreach`, `loop`).
- Async/Await: `async` and `await` constructs.
- Exception Handling: `try-catch` blocks and exception handling.
- Variables and Scoping: Local variables, block scoping, and variable assignment.
- Expressions and Methods: Lambdas, inline functions, and method calls.
- Generics: Fully supports generic types and methods.
- LINQ: Seamless integration with LINQ expressions for dynamic query building.
- Tuples and Deconstruction: Built-in support for tuples and destructuring patterns.
- Debugging: Debug statements and detailed syntax error reporting.

#### **Extensibility**:

XS allows developers to add custom expressions, keywords, operators, and statements through its `IParseExtension` interface.

XS's extensibility makes it future-proof. By allowing developers to easily add constructs tailored to their domain, XS 
ensures that the language can evolve with changing requirements. Unlike other tools that lock developers into fixed 
capabilities, XS encourages customization and innovation.

Example Extensions:

- `async` `await` support.
- `for`, `foreach`, and `while`.
- `using` blocks.
- String interpolation.
- Null coalescing (`??`).

#### **Debugging**:

XS supports `debug();` statements and line level debugging, allowing users to create debug callbacks and set breakpoints. 
This feature automates a process that is laborious when using manual expression trees.

```
var debugger = new XsDebugger()
{
    Handler = d =>
    {
        Console.WriteLine( $"Line: {d.Line}, Column: {d.Column}, Variables: {d.Variables}, Text: {d.SourceLine}" );
    }
};

var expression = Xs.Parse( script, debugger );

var lambda = Expression.Lambda<Func<List<int>>>( expression );
var compiled = lambda.Compile();
var result = compiled();
```

---

## **Getting Started with XS**

To get started with XS, you need to set up a .NET project. Ensure you have .NET 8 or .NET 9 installed.

Add the necessary packages:

```
dotnet add package Hyperbee.XS
dotnet add package Hyperbee.XS.Extensions
```

Create an XS script:

```csharp
var config = new XsConfig { Extensions = Hyperbee.Xs.Extensions.XsExtensions };
var parser = new XsParser( config );

var expression = XsParser.Parse(
    """
    var array = new int[] { 1,2,3 };
    var x = 0;
    
    foreach ( var item in array )
    {
      x += item;
    }

    x + 12; // return the last expression
    """ 
);

var lambda = Lambda<Func<int>>( expression );

var compiled = lambda.Compile();
var result = compiled();

Assert.AreEqual( 42, result );
```

---

## **Additional Features**

### **ToExpressionString()**

XS provides a `ToExpressionString()` method that converts an expression tree into a string representing C# expression syntax.  For example:

```csharp
var expression = XsParser.Parse( "1 + 2 * 3;" );
var expressionString = expression.ToExpressionString();
```

would output:

```csharp
var expression = Expression.Add( 
  Expression.Constant( 1 ), 
  Expression.Multiply( 
    Expression.Constant( 2 ), 
    Expression.Constant( 3 ) 
  ) 
);
```

### **ToXS()**

XS also provides a `ToXS()` method that converts an expression tree into an XS script. For example:


```csharp
var expression = Expression.Add( 
  Expression.Constant( 1 ), 
  Expression.Multiply( 
    Expression.Constant( 2 ), 
    Expression.Constant( 3 ) 
  ) 
);
var xs = expression.ToXS();
```

would output:

```csharp
1 + 2 * 3;
```

> Note: it is not guaranteed that the output will be the same as the original script, but it will be semantically 
equivalent. For example whitespace, variable names and block closure may differ.

---

### **Conclusion**

XS addresses a gap in the .NET ecosystem by simplifying the creation and management of expression trees. With its 
lightweight design, and advanced extensibility, XS helps developers build robust, dynamic systems with minimal overhead.

---

## Credits

Special thanks to:

- [Parlot](https://github.com/sebastienros/parlot) for the fast .NET parser combinator. :heart:
- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.
