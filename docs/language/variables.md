---
layout: default
title: Variables
parent: Language
nav_order: 20
---

# Variables

Variables in XS are used to store data that can be referenced and manipulated in a program. 
In XS, variables are declared using the `var` keyword.

## Syntax

```abnf
; Variable Declarations
declaration = "var" identifier [ "=" expression ]

identifier = ALPHA *(ALPHA / DIGIT / "_")
```

## Examples

```xs
var answer = 42;
var question = "What is the meaning of life, the universe, and everything?";
var result = `The answer to '{question}' is {answer}.`;

result; // XS returns the last evaluated expression
```
