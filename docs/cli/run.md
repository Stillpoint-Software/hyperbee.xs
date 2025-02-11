---
layout: default
title: Run
parent: Cli
nav_order: 4
---

# Run

The `run` command is used to execute a file or script.

## Usage

`xs run [OPTIONS] <COMMAND>`

### Options

| Option             | Description                                 |
|--------------------|---------------------------------------------|
| `-h, --help`       | Prints help information                     |
| `-r, --references` | Add references to the compilation context   |
| `-p, --packages`   | Add packages to the compilation context     |
| `-e, --extensions` | Add extensions to the compilation context   |
| `-s, --show`       | Show the expression tree instead of running |

### Commands

| Command  | Description                               |
|----------|-------------------------------------------|
| `file`   | Run a file and see the results            |
| `script` | Run a script and see the results          |

## Examples

### Example Run File
```
xs run file ./path/to/file.xs
```

### Example Run Script

```
xs run script [script]
```
> Script is optional, if omitted the command will start single use shell
> 
> ```
> xs run script
>
> script: (type "run" to execute or "show" to see C# expression tree)
> > var s = "hello";
> > s;
> > run
> Result: hello
> ```

### Example Show Expression Tree

```
xs run script "5+5;" -s
```

> This outputs the code for a C# expression tree instead of running the script
> ```
> using System;
> using System.Linq.Expressions;
>
> var expression = Expression.Add(
>   Expression.Constant(5),
>   Expression.Constant(5)
> );
> ```