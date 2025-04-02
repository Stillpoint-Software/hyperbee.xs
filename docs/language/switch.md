---
layout: default
title: Switch Statements
parent: Language
nav_order: 16
---

# Switch

The `switch` statement evaluates an expression and executes the corresponding case block based on the value of the expression.

## Usage

```
var x = 42;
var result = switch (x) {
    case 1: "One";
    case 42: "The answer to life, the universe, and everything";
    default: "Other";
};
result;
```