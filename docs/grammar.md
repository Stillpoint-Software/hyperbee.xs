---
layout: default
title: Grammar
nav_order: 5
---
# Grammar

## ABNF Grammar Specification

Wroking draft of the ABNF grammar specification for the language.

```abnf
; Root Rule: A script is a sequence of statements
script              = *statement

; Statements
statement           = complex-statement
                    / terminated-statement
                    / label-statement

complex-statement   = conditional
                    / loop
                    / try-catch
                    / switch
                    / block

terminated-statement = break-statement
                      / continue-statement
                      / goto-statement
                      / return-statement
                      / throw-statement
                      / expression-statement

expression-statement = assignable-expression *( ";" )

label-statement     = identifier ":"

; Methods
method              = ["async"] "function" identifier "(" [ parameter-list ] ")" block

; Assignable Expressions
assignable-expression = declaration
                     / assignment
                     / expression

; Declarations
declaration         = "var" identifier [ "=" expression ]

; Assignments
assignment          = identifier assignment-operator expression
assignment-operator = "=" / "+=" / "-=" / "*=" / "/=" / "%=" / "**=" / "??="

; Expressions
expression          = literal
                    / identifier
                    / unary-expression
                    / binary-expression
                    / grouped-expression
                    / new-expression
                    / lambda-expression
                    / method-call
                    / string-interpolation
                    / cast-expression

cast-expression     = primary-expression ("is" typename / "as" typename / "as?" typename)

string-interpolation = backtick *( interpolation-content ) backtick
interpolation-content = (%x20-7E / "{" expression "}")
backtick            = %x60

grouped-expression  = "(" expression ")"

; Literals
literal             = integer-literal / float-literal / double-literal
                    / long-literal / string-literal / boolean-literal / char-literal / "null"

integer-literal     = DIGIT1 *(DIGIT) ["N"]
float-literal       = DIGIT1 *(DIGIT) "." *(DIGIT) "F"
double-literal      = DIGIT1 *(DIGIT) "." *(DIGIT) "D"
long-literal        = DIGIT1 *(DIGIT) "L"
string-literal      = DQUOTE *(%x20-21 / %x23-7E) DQUOTE
boolean-literal     = "true" / "false"
char-literal        = SQUOTE %x20-7E SQUOTE

; Unary Expressions
unary-expression    = ("!" / "-" / "++" / "--") primary-expression

; Binary Expressions
binary-expression   = primary-expression binary-operator primary-expression
binary-operator     = "**" / "%" / "*" / "/" / "+" / "-"
                    / "==" / "!=" / "<" / ">" / "<=" / ">="
                    / "&&" / "||" / "??"
                    / "&" / "|" / "^" / "<<" / ">>"

; Primary Expressions
primary-expression  = literal
                    / identifier
                    / grouped-expression
                    / block

; New Expression
new-expression      = "new" typename "(" [ argument-list ] ")"

; Lambda Expressions
lambda-expression   = "(" [ lambda-parameter-list ] ")" "=>" block
lambda-parameter-list = typename identifier *( "," typename identifier )

; Method Calls
method-call         = identifier "(" [ argument-list ] ")"
                    / generic-method-call

generic-method-call = identifier "<" type-argument-list ">" "(" [ argument-list ] ")"
type-argument-list  = typename *( "," typename )

; Control Flow
conditional         = "if" "(" expression ")" (terminated-statement / block) [ "else" (terminated-statement / block) ]
loop                = "loop" block
break-statement     = "break"
continue-statement  = "continue"
goto-statement      = "goto" identifier
return-statement    = "return" [expression]
throw-statement     = "throw" [expression]

; Try-Catch-Finally
try-catch           = "try" block *(catch-clause) ["finally" block]
catch-clause        = "catch" "(" typename [identifier] ")" block

; Switch
switch              = "switch" "(" expression ")" "{" *case-statement [default-statement] "}"
case-statement      = "case" expression ":" *statement
default-statement   = "default" ":" *statement

; Blocks
block               = "{" *statement "}"

; Identifiers and Typenames
identifier          = ALPHA *(ALPHA / DIGIT / "_")
typename            = identifier *( "." identifier ) [generic-arguments]
generic-arguments   = "<" typename *( "," typename ) ">

; Arguments
argument-list       = expression *( "," expression )

; Miscellaneous
DIGIT               = %x30-39
DIGIT1              = %x31-39
ALPHA               = %x41-5A / %x61-7A
DQUOTE              = %x22
SQUOTE              = %x27
```
