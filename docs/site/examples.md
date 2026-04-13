---
layout: default
title: Examples
nav_order: 2
---

# Examples

```
var results = new List<int>(5);

var c = 0; var c1 = 0; var c2 = 0;

// conditional
if (1 + 1 == 2)
{
    c = if (true) { 42; } else { 0; };
}
else
{
    c = 1;
}

results.Add(c);

// switch
var s = 3;
switch (s)
{
    case 1: 
       s = 1; 
       goto there;
    case 2: 
       s = 2; 
       goto there;
    default: 
       s = 42; 
       goto there;
}
there:

results.Add(s);

// try-catch-finally
var t = 1;

try
{
    throw new ArgumentException();
}
catch (InvalidOperationException)
{
    t = 0;
}
catch (ArgumentException)
{
    t += 40;
}
finally
{
    t += 1;
}

results.Add(t);

// loop
var l = 0;
loop
{
    l++; 
    if( l == 42 )
    {
        break;
    }
}
results.Add(l);

// lambda
var calc = (int a, int b) => a * b;
results.Add( calc(6, 7) );

results;
```
