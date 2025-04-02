---
layout: default
title: Yield
parent: Language
nav_order: 24
---

# Yield

The `yield` within an `enumerable` block allows you to return a value from a function and then continue execution. The `break` keyword can be used to exit the block early.

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```
enumerable {
    yield 1;
    yield 2;
    break;
    yield 3;
}
```
