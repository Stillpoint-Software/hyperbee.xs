---
layout: default
title: Method Calls
parent: Language
nav_order: 10
---

# Method Calls

Method calls are used to invoke methods on objects. You can pass arguments to the method and receive a return value.

## Syntax

```abnf
; Method Calls
method-call = identifier "(" [ argument-list ] ")" / generic-method-call

generic-method-call = identifier "<" type-argument-list ">" "(" [ argument-list ] ")"

argument-list = expression *( "," expression )
type-argument-list = typename *( "," typename )
generic-arguments = "<" typename *( "," typename ) ">"
```

## Examples

```xs
var myObject = new MyClass();
var result = myObject.MyMethod(42, "Hitchhiker's Guide");
result;
```
