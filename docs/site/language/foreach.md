---
layout: default
title: Foreach
parent: Language
nav_order: 7
---

# Foreach Loop

The `foreach` loop is used to iterate over a collection and execute a block of code for each item. 

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```
var list = [1, 2, 3];
foreach (var item in list) {
    item;
}
```
