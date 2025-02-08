---
layout: default
title: Try-Catch-Finally
parent: Language
nav_order: 8
---

## Description

The `try` block contains code that may throw an exception. The `catch` block contains code that handles the exception if one is thrown.
The `finally` block contains code that is always executed after the `try` block, regardless of whether an exception was thrown.

## Syntax

```abnf
; Try-Catch-Finally
try-catch = "try" block *(catch-clause) ["finally" block]

catch-clause = "catch" "(" typename [identifier] ")" block

block = "{" *statement "}"
typename = identifier *( "." identifier ) [generic-arguments]
```

## Examples

```xs
try {
    throw new Exception("An error occurred");
} catch (Exception e) {
    e.Message;
}
```