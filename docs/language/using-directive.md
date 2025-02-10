---
layout: default
title: Using Directive
parent: Language
nav_order: 19
---

# Using Directive

The `using` directive allows the use of types in a namespace so that you do not have to fully qualify them.

## Syntax

```abnf
; Using Directive
using-namespace = "using" namespace-identifier ";"
```

## Examples

```xs
using namespace;
```