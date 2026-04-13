---
layout: default
title: String Interpolation
parent: Language
nav_order: 15
---

# String Interpolation

String interpolation is a way to construct strings by embedding expressions within string literals. XS uses backtick-delimited strings for interpolation.

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```
var value = 42;
var myString = `The answer to life, the universe, and everything is {value}.`;
```
