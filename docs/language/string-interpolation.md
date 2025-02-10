---
layout: default
title: String Interpolation
parent: Language
nav_order: 13
---

# String Interpolation

String interpolation is a way to construct strings by embedding expressions within string literals. XS uses backtick-delimited strings for interpolation.

## Syntax

```abnf
; String Interpolation
string-interpolation = backtick *( interpolation-content ) backtick

interpolation-content = (%x20-7E / "{" expression "}")
backtick = %x60
```

## Examples

```xs
var value = 42;
var myString = `The answer to life, the universe, and everything is {value}.`;
```
