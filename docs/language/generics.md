---
layout: default
title: Generics
parent: Language
nav_order: 7
---

# Generics

Generics in XS allow you to define classes, methods, and data structures with a placeholder for the type. This enables you to create reusable and type-safe code components.

To create a generic type, use the following syntax:

```xs
var list = new List<type> { value1, value2, value3 };
```

Example:

```xs
var list = new List<int> { 1, 2, 39 };
var sum = 0;
foreach (var item in list) {
    sum += item;
}
sum;
```
