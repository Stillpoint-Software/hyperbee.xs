---
layout: default
title: Tuples
parent: Language
nav_order: 18
---

# Tuples

Tuples in XS allow you to store multiple values in a single variable. They are useful for returning multiple values from a method or grouping related values together.

## Usage

To create a tuple, use the following syntax:

```
var tuple = (value1, value2, value3);
```

You can access the elements of a tuple using the Item1, Item2, etc., properties:

```
var tuple = (42, "Hitchhiker's Guide", true);
var first = tuple.Item1;  // 42
var second = tuple.Item2; // "Hitchhiker's Guide"
var third = tuple.Item3;  // true
```
