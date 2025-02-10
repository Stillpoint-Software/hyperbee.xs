---
layout: default
title: Async-Await
parent: Language
nav_order: 2
---

# Async/Await

Async blocks are used to write asynchronous code. They allow you to await tasks without blocking the main thread.

## Syntax

```abnf
; Async and Await Expressions
async-block         = "async" block
await-expression    = "await" expression

block               = "{" *statement "}"
```

## Examples

```xs
async {
    await Task.Delay(1000);
    // code to execute after delay
}
```

