---
layout: default
title: Loop
parent: Language
nav_order: 11
---

# Loop

The `loop` construct is used to create an infinite loop that can be terminated using a `break` statement.

## Usage

```
var x = 0;
loop {
    x++;
    if (x == 42) {
        break;
    }
}
print(x);
```