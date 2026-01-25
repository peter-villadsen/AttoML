# Operators

AttoML supports a set of infix operators. The parser rewrites them to calls over built-in functions to keep typing and evaluation simple and consistent.

Overview:
- Arithmetic: `+`, `-`, `*`, `/` → `Base.add/sub/mul/div` (works for `int` and `float`)
- Integer arithmetic: word operators `div`, `mod` → `Base.idiv` and `Base.mod` (integers only)
- Relational: `<`, `>`, `<=`, `>=` → combinations of `Base.lt`, `Base.eq`, and boolean ops (works for `int` and `float`)
- Equality: `=` (structural) → `Base.eq`; inequality `<>` → `Base.not (Base.eq ...)`
- Boolean short-circuit: `andthen`, `orelse` → rewritten to `if then else` for short-circuit evaluation
- List append: `@` → `List.append`

Notes:
- Integer divide-by-zero: `1 / 0`, `10 div 0`, and `10 mod 0` raise the `Div` exception. Floating-point division follows IEEE-754 and does not raise.
- Structural equality `=` compares deep structure across tuples, lists, records, and ADT values.
- `andthen` evaluates the right-hand side only if the left-hand side is `true`; `orelse` evaluates the right-hand side only if the left-hand side is `false`.
- Precedence: arithmetic binds tighter than relational/equality; both sides of relational operators are parsed as full applications, so `1 + 2 < 4 + 5` parses as `(1 + 2) < (4 + 5)`. Parentheses are recommended for clarity with complex expressions.

Examples:
```
1 + 2 * 3        // 7
10 div 3         // 3
10 mod 3         // 1
x / 0 handle Div -> 42 // integer zero division
x <= y           // bool
[a] @ [b, c]     // concatenation
true andthen f() // f() runs only if left is true
u = {x = 1}      // structural equality over records
None <> Some 1   // inequality of ADT constructors
```
