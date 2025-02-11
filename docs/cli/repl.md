---
layout: default
title: Compile
parent: Cli
nav_order: 3
---

# Repl

The `repl` (Read, Eval, Print, and Loop) command is used to start the interactive shell.

## Usage

```xs repl [OPTIONS]```

### Options

| Option             | Description                               |
|--------------------|-------------------------------------------|
| `-h, --help`       | Prints help information                   |
| `-r, --references` | Add references to the compilation context |
| `-p, --packages`   | Add packages to the compilation context   |
| `-e, --extensions` | Add extensions to the compilation context |

## Examples

### Example Start Repl
```
xs repl
```

### Example Start Repl with References
```
xs repl -r ./path/to/reference.dll
```

### Example Start Repl with Packages
```
xs repl -p Humanizer.Core
...
> var x = 1234;
> x.ToWords();
> run
Result:
one thousand two hundred and thirty-four
```

### Example Start Repl with Extensions
```
xs repl -p <NuGet> -e <ExtensionName>
```
> ExtensionName must already be loaded using a reference or package