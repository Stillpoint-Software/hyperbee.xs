# XS.Cli: Cli tooling for Hyperbee.XS

### **What is XS?**

[XS](https://github.com/Stillpoint-Software/hyperbee.xs) is a lightweight scripting language designed to simplify and enhance the use of C# expression trees.
It provides a familiar C#-like syntax while offering advanced extensibility, making it a compelling choice for developers
building domain-specific languages (DSLs), rules engines, or dynamic runtime logic systems.

XS.Cli added dotnet commands like:

- run
- compile
- repl

### Install Xs.Cli using 

you can install Xs.Cli using the following command:
```
dotnet tool install -g hyperbee.xs.cli
```
or following Microsoft's [documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)

### Run
```
xs run script
```
or:
```
xs run file ./script.xs
```

### Compile (.NET 9 or later)
```
xs compile -s "1 + 1" -o "output.dll"
```

### Repl
```
xs repl
```

## Examples

### Run a script
```
xs run script "(1-5);"
Result: -4
```

### Show Expression Tree
```
xs run script "if( true ) 1+1; else 0;" -s
Result:
using System;                                                                                                                                                  │
using System.Linq.Expressions;                                                                                                                                 │
                                                                                                                                                                │
var expression = Expression.Condition(                                                                                                                         │
  Expression.Constant(true),                                                                                                                                   │
  Expression.Add(                                                                                                                                              │
    Expression.Constant(1),                                                                                                                                    │
    Expression.Constant(1)                                                                                                                                     │
  ),                                                                                                                                                           │
  Expression.Constant(0),                                                                                                                                      │
  typeof(Int32)                                                                                                                                                │
);
```

### Run a repl session
```
xs repl
Starting REPL session. Type "run" to run the current block, "exit" to quit, "print" to see variables.

> print
┌──────┬───────┐
│ Name │ Value │
└──────┴───────┘
> var x = 2;
> var y = 5;
> x + y;
> run
Result:
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ 7                                                                                   │
└─────────────────────────────────────────────────────────────────────────────────────┘
> print
┌──────┬───────┐
│ Name │ Value │
├──────┼───────┤
│ x    │ 2     │
│ y    │ 5     │
└──────┴───────┘
> exit
```

## Credits

Special thanks to:

- [Spectre.Console](https://spectreconsole.net/) for the beautiful console and command line tools. :heart:
- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.


## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.
