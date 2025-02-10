---
layout: default
title: Conditionals
parent: Language
nav_order: 4
---

# Conditionals

The `if` statement is used to perform different actions based on different test conditions. 

## Syntax

```abnf
; Conditional Statements
conditional = "if" "(" expression ")" (terminated-statement / block) [ "else" (terminated-statement / block) ]
block       = "{" *statement "}"
```

## Examples

```xs
if (x == 42) {
    "The answer to everything.";
} else {
    "Just a number.";
};
```

```xs
var result = if (x == 42) {
    "The answer to everything.";
} else {
    "Just a number.";
};
result;
```