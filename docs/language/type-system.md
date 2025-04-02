---
layout: default
title: Type System
parent: Language
nav_order: 19
---

# Type System

## **Primitive Types**

| Type      | Suffix | Example              |
| --------- | ------ | -------------------- |
| `int`     | `n`    | `var x = 42n`        |
| `float`   | `f`    | `var pi = 3.14f`     |
| `double`  | `d`    | `var e = 2.718d`     |
| `long`    | `l`    | `var big = 10000l`   |
| `short`   | `s`    | `var small = 123s`   |
| `decimal` | `m`    | `var price = 19.99m` |
| `string`  | N/A    | `var name = "hello"` |
| `char`    | N/A    | `var c = 'x`         |
| `bool`    | N/A    | `var flag = true`    |
| `null`    | N/A    | `var value = null`   |

## **Type Promotion Rules**

1. Arithmetic operations promote types:  
   `int` → `float` → `double` → `decimal`.
2. Null-coalescing (`??`) resolves to the type of the non-null operand.
3. Conditional expressions (`if-else`) return the common compatible type.
