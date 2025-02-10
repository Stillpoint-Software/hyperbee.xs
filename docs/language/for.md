---
layout: default
title: For
parent: Language
nav_order: 5
---

# For Loop

The `for` loop is used to repeat a block of code a specific number of times. This construct requires the `Hyperbee.XS.Extensions` package.

## Syntax

```abnf
; For Loop
for-loop = "for" "(" for-init ";" expression ";" for-update ")" (terminated-statement / complex-statement)

for-init        = [declaration / expression-list]
for-update      = [expression-list]
expression-list = expression *( "," expression )
```

## Examples

```xs
for (var i = 0; i < 42; i++) {
    print(i);
}
```