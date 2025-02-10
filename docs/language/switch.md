---
layout: default
title: Switch Statements
parent: Language
nav_order: 14
---

# Switch

The `switch` statement evaluates an expression and executes the corresponding case block based on the value of the expression.

## Syntax

```abnf
; Switch
switch = "switch" "(" expression ")" "{" *case-statement [default-statement] "}"

case-statement = "case" expression ":" *statement
default-statement = "default" ":" *statement
```

## Examples

```xs
var x = 42;
var result = switch (x) {
    case 1: "One";
    case 42: "The answer to life, the universe, and everything";
    default: "Other";
};
result;
```