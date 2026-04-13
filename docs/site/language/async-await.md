---
layout: default
title: Async-Await
parent: Language
nav_order: 2
---

# Async/Await

Async blocks are used to write asynchronous code. They allow you to await tasks without blocking the main thread.

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```
async {
    await Task.Delay(1000);
    // code to execute after delay
}
```

