---
layout: default
title: Debugging
nav_order: 4
---

# Debugging

XS supports debug expressions (breakpoints), and statement debugging.

## Description

Debug expressions are used to set breakpoints directly into your scripts. They can be used to conditionally or unconditionally debug your code.
You can also enable statement debugging in the XS configuration to enable statement debugging. In this case, the debugger will be called for every statement in the script.

## Syntax

```xs
debug(expression); // Conditional debug breakpoint
debug();           // Unconditional debug breakpoint
```

## Examples

```xs
var x = 42;
debug(x == 42); // Conditional debug breakpoint
debug();        // Unconditional debug breakpoint
```

## Detailed Example

### Example `XsConfig` Setup

```csharp
var config = new XsConfig
{
    Debugger = (line, column, variables) =>
    {
        Console.WriteLine($"Debugging at Line: {line}, Column: {column}");
        foreach (var kvp in variables)
        {
            Console.WriteLine($"Variable {kvp.Key} = {kvp.Value}");
        }
    }
};
```

### Debugging Syntax

```xs
var x = 42;
debug(x == 42); // Conditional debug breakpoint
debug();        // Unconditional debug breakpoint
```

### Complex Debugging Example

```xs
var results = new List<int>(5);

debug(); 

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