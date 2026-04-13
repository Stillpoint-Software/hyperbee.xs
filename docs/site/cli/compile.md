---
layout: default
title: Compile
parent: Cli
nav_order: 2
---

# Compile

The `compile` command is used to compile a file to an assembly.

## Usage

```xs compile <File> [OPTIONS]```

### Arguments

| Argument | Description     |
|----------|-----------------|
| `file`   | File to compile |

### Options

| Option               | Description                                                       |
|----------------------|-------------------------------------------------------------------|
| `-h, --help`         | Prints help information                                           |
| `-r, --references`   | Add references to the compilation context                         |
| `-p, --packages`     | Add packages to the compilation context                           |
| `-e, --extensions`   | Add extensions to the compilation context                         |
| `-o, --output`       | File path for the saved assembly                                  |
| `-a, --assemblyName` | Assembly Name (can include Version, Culture and PublicKeyToken)   |
| `-m, --module`       | Module Name                                                       |
| `-c, --class`        | Class Name                                                        |
| `-f, --function`     | Function Name                                                     |

## Examples

### Example Compile File
```
xs compile "./path/to/file.xs" -o "./path/to/output.dll" -a "MyAssembly, Version=1.0.0.0, Culture=neutral" -m MyModule -c MyClass -f MyFunction
```
> -m, -c, -f are optional, if omitted will be DynamicModule, DynamicClass, DynamicMethod; respectively