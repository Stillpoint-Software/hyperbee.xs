---
layout: default
title: Debugging
nav_order: 4
---

# Debugging

XS supports debug expressions (breakpoints), and statement debugging.

Debug expressions are used to set breakpoints directly into your scripts. They can be used to conditionally or unconditionally debug your code.
You can also enable statement debugging in the XS configuration to enable statement debugging. In this case, the debugger will be called for every statement in the script.

## Breakpoints

```xs
debug(expression); // Conditional debug breakpoint
debug();           // Unconditional debug breakpoint
```

## Example Usage 1

This example sets up the debugger to break on every `debug()` call in the script.

### Debug Breakpoint Calls
```xs
var x = 42;
debug(x == 42); // Conditional debug breakpoint
debug();        // Unconditional debug breakpoint
```

### `XsDebugger` Setup

```csharp
var debugger = new XsDebugger()
{
    BreakMode = BreakMode.Call,  // DEBUG ON `debug()` CALLS
    Callback = x =>
    {
        Console.WriteLine($"Debugging at Line: {x.Line}, Column: {x.Column} - {x.SourceLine}");

        foreach (var kvp in x.Variables)
        {
            Console.WriteLine($"Variable {kvp.Key} = {kvp.Value}");
        }
    }
};

var expression = Xs.Parse( script, debugger );

var lambda = Expression.Lambda<Func<int>>( expression );
var compiled = lambda.Compile();
var result = compiled();
```

## Example Usage 2

This example sets up the debugger to break on every statement in the script.

### Debug Statement Stepping
```xs
var results = new List<int>(5);

var c = 0;
if (1 + 1 == 2)
{
    c = if (true) { 
        42; 
    } 
    else { 
        0; 
    };
}
else
{
    c = 1;
}
results.Add(c);

results;
```

### `XsDebugger` Setup

```csharp
var debugger = new XsDebugger()
{
    BreakMode = BreakMode.Statements, // DEBUG STATEMENTS
    Callback = x =>
    {
        Console.WriteLine($"Debugging at Line: {x.Line}, Column: {x.Column} - {x.SourceLine}");

        foreach (var kvp in x.Variables)
        {
            Console.WriteLine($"Variable {kvp.Key} = {kvp.Value}");
        }
    }
};

var expression = Xs.Parse( script, debugger );

var lambda = Expression.Lambda<Func<int>>( expression );
var compiled = lambda.Compile();
var result = compiled();
```
