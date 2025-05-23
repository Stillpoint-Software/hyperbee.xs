---
layout: default
title: Operators
parent: Language
nav_order: 13
---

# Operators

## Unary Operators

```
var isPositive = +x;
var isNegative = -x;
var isNot = !x;
var isTrue = ?(1 == 1);
var isFalse = !?(true);
```

## Binary Operators

```
var sum = x + y;       // Addition
var diff = x - y;      // Subtraction
var product = x * y;   // Multiplication
var quotient = x / y;  // Division
var remainder = x % y; // Modulo
var power = x ** y;    // Exponentiation
```

## Comparison Operators

```
var isEqual = x == y;           // Equality
var isNotEqual = x != y;        // Inequality
var isGreater = x > y;          // Greater than
var isLess = x < y;             // Less than
var isGreaterOrEqual = x >= y;  // Greater than or equal
var isLessOrEqual = x <= y;     // Less than or equal
```

## Logical Operators

```
var andResult = a && b;        // Logical AND
var orResult = a || b;         // Logical OR
var notResult = !a;            // Logical NOT
```

## Bitwise Operators

```
var xorResult = x ^ y;       // Bitwise XOR 
var andResult = x & y;       // Bitwise AND 
var orResult = x | y;        // Bitwise OR 
var leftShift = x << y;      // Left Shift 
var rightShift = x >> y;     // Right Shift
```

## Prefix and Postfix Operators

```
var postinc = x++;           // Post-increment
var postdec = x--;           // Post-decrement
var preinc = ++x;            // Pre-increment
var predec = --x;            // Pre-decrement
```

## Null Coalescing

```
var result = a ?? b;        // 'a' if not null, otherwise 'b'
```

## Assignment Operators

```
var x = 10;     // Simple assignment
x += 5;         // Addition assignment
x -= 3;         // Subtraction assignment
x *= 2;         // Multiplication assignment
x /= 4;         // Division assignment
x %= 3;         // Modulo assignment
x **= 2;        // Exponentiation assignment
x ??= y;        // Null-coalescing assignment

x ^= y;         // XOR assignment
x &= y;         // AND assignment
x |= y;         // OR assignment
x >>= 1;        // Left shift assignment
x <<= 1;        // Right shift assignment
```

