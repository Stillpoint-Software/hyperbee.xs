---
layout: default
title: Casting
parent: Language
nav_order: 3
---

# Casting

Casting is used to convert a value from one type to another. XS supports direct casting, type checking, and safe casting.

## Usage

```
var value = 42L;
var cast = value as int;
var isInt = value is int;
var safeCast = value as? int;
```
