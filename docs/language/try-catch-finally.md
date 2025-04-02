---
layout: default
title: Try-Catch-Finally
parent: Language
nav_order: 17
---

# Try-Catch-Finally

The `try` block contains code that may throw an exception. The `catch` block contains code that handles the exception if one is thrown.
The `finally` block contains code that is always executed after the `try` block, regardless of whether an exception was thrown.

## Usage

```
try {
    throw new Exception("An error occurred");
} 
catch (Exception e) {
    e.Message;
}
finally {
    // Finally block
}
```