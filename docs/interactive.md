---
layout: default
title: Interactive
nav_order: 6
---

# Interactive (Polyglot Notebooks)

This document provides an overview of the XS Interactive for [Polyglot Notebooks](https://code.visualstudio.com/docs/languages/polyglot). 
:notebook:

## Install

To install `Hyperbee.XS.Interactive` for a notebook using the `#r`:

```
#r "nuget:Hyperbee.XS"
#r "nuget:Hyperbee.XS.Extensions"
#r "nuget:Hyperbee.XS.Interactive"
```

> You can also install a local copy using the `#i` command.
> ```
> #i "nuget:local/nuget/path"
> ```

## Supported Features

### Kernel Commands

| Command     | Description                                   |
|-------------|-----------------------------------------------|
| `#!xs`      | Switch to the XS kernel                       |
| `#!xs-show` | Switch to the Show (Outputs Expression Trees) |

### Directives

| Command    | Description                               |
|------------|-------------------------------------------|
| `#r`       | Add references to the compilation context |
| `#i`       | Add packages to the compilation context   |

### Magic Commands

| Command    | Description                               |
|------------|-------------------------------------------|
| `#!share`  | Share a variable with the current context |
| `#!whos`   | List all variables in the current context |
| `#!with`   | Enables adding custom extensions          |

### Build-in Extensions

| Code               | Description                               |
|--------------------|-------------------------------------------|
| `package <name>`   | Import a package                          |
| `source <name>`    | Local location for nuget packages         |
| `display(<expr>)`  | Display the result of an expression       |


### Examples

#### Adding External Packages

```
#!xs

package Humanizer.Core;
using Humanizer;

var x = 1+5;

x.ToWords();
```

#### Displaying Values

```
#!xs

using System.Collections.Generic;
var dictionary = new Dictionary<string, int>();

dictionary["x"] = x;
dictionary["y"] = 42;

display( dictionary, "application/json" );
display( 5.ToString() );
```

#### Showing Expression Trees
```
#!xs-show

if( true ) 1+5; else 0;
```

> Cell Output:
> ```
> using System;
> using System.Linq.Expressions;
> 
> var expression = Expression.Condition(
>   Expression.Constant(true),
>   Expression.Add(
>     Expression.Constant(1),
>     Expression.Constant(5)
>   ),
>   Expression.Constant(0),
>   typeof(Int32)
> );
> ```

#### Variable Sharing

CSharp Kernel:
```csharp
var simple = "hello";

class Person {
    public string Name { get; set; }
    public int Age { get; set; }
}

var complex = new Person { Name = "John", Age = 30 };
```

Shared with XS Kernel:
```
#!xs
#!share --from csharp --name "simple" --as "xSimple"
#!share --from csharp --name "complex" --as "xComplex"

display(xSimple);
xComplex.Name;
```

#### Using Custom Extensions

Add the custom extension's NuGet to the XS kernel:
```
#!xs
package CustomNuget.RepeatExpression;
```

Enable the extension using the `#!with` magic command:
```
#!xs
#!with --extension "RepeatExpression"

repeat(5) {
    display("Hello");
}
```