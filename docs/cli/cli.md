---
layout: default
title: Cli
has_children: true
nav_order: 1
---

# Hyperbee.XS.Cli

This document provides an overview of the XS Cli tooling.

## Install

To install `Hyperbee.XS.Cli` as a global command line tool:

```
dotnet tool install -g hyperbee.xs.cli
```

or following Microsoft's [documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)

## Usage

```xs [OPTIONS] <COMMAND>```

### Options

| Option             | Description                               |
|--------------------|-------------------------------------------|
| `-h, --help`       | Prints help information                   |


### Commands

| Command  | Description                               |
|----------|-------------------------------------------|
| `repl`   | Start an interactive shell                |
| `compile`| Compile a file to an assembly             |
| `run`    | Run a file and see the resuls             |
