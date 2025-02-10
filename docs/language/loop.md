---
layout: default
title: Loop
parent: Language
nav_order: 9
---

# Loop

The `loop` construct is used to create an infinite loop that can be terminated using a `break` statement.

## Syntax

```abnf
; Loop
loop = "loop" block

block = "{" *statement "}"
```

## Examples

```xs
var x = 0;
loop {
    x++;
    if (x == 42) {
        break;
    }
}
print(x);
```