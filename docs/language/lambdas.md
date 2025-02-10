---
layout: default
title: Lambdas
parent: Language
nav_order: 8
---

# Lambdas

Lambdas are anonymous functions that can be used to create inline functions. They are useful for short, simple functions.

## Syntax

```abnf
; Lambda Expressions
lambda-expression = "(" [ lambda-parameter-list ] ")" "=>" (terminated-statement / complex-statement)

lambda-parameter-list = typename identifier *( "," typename identifier )
block = "{" *statement "}"
```

## Examples

```xs
var add = (int x, int y) => x + y;
var result = add(42, 10);
result;
```
