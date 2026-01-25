# Exceptions

AttoML models exceptions explicitly in the language with a built-in type `exn`, exception constructors, and `raise`/`handle` expressions. The interpreter uses a small bridge exception (AttoException) internally to transport AttoML exceptions through host (.NET) control flow.

## Language Surface
- Type: `exn` — the type of exceptions.
- Declaring constructors:
  - `exception Div`
  - `exception Domain`
  - `exception Fail of string`
  - User-defined: `exception Oops of int`, etc.
- Raising: `raise expr` where `expr : exn`.
- Handling: `expr handle pat1 -> e1 | pat2 -> e2 | ...`.

Patterns inside `handle` are the same patterns used in `match` (constructor patterns can have payloads).

## Built-in Exceptions
- `Div : exn` — integer divide-by-zero for `/`, `div`, and `mod`.
- `Domain : exn` — invalid mathematical domains, e.g., `Math.sqrt x` when `x < 0` and `Math.log x` when `x <= 0`.
- `Fail : string -> exn` — general failure with a message.

These constructors are available by default in the interpreter environment.

## Semantics
- `raise e` has type `'a` for any `'a`, provided `e : exn`. This lets it appear in any expression position.
- `expr handle pat -> e | ...` evaluates `expr`; if it raises an exception `ex`, the patterns are tried in order. The first successful match runs its branch. The overall type is the same as `expr` and all branches.
- Exceptions interoperate naturally with pattern matching:
  - Example: `(raise (Fail "nope")) handle Fail s -> s` evaluates to `"nope"`.

## Operator and Library Exceptions
- Integer division/modulus zero divisor raises `Div`:
  - `(1 / 0)`, `(10 div 0)`, `(10 mod 0)`.
- Floating-point division uses IEEE-754 and does not raise exceptions.
- `Math.sqrt x` raises `Domain` when `x < 0`.
- `Math.log x` raises `Domain` when `x <= 0`.

## Why AttoException?
The interpreter is implemented in C#, but AttoML exceptions are values of type `exn` (e.g., `<Div>`, `<Fail "...">`). When AttoML code raises an exception, the interpreter throws a dedicated host exception `AttoException` that carries the `exn` value. This is necessary to:
- Preserve the AttoML exception value across host frames without lossy conversion.
- Distinguish language-level exceptions from accidental host errors (e.g., a bug in the interpreter should be a normal .NET exception, not an AttoML `exn`).
- Implement `handle` by catching a single known host exception type and pattern matching on its payload value.

In short: AttoException is a transport wrapper so AttoML `exn` values can unwind the host stack and be caught by `handle` without conflating them with unrelated runtime errors.

## Examples
```
exception Oops of int
(raise (Oops 3)) handle Oops n -> n     // 3

(1 / 0) handle Div -> 42                // 42
(10 div 0) handle Div -> 42             // 42
(10 mod 0) handle Div -> 42             // 42

(Math.sqrt (0.0 - 1.0)) handle Domain -> 0.0  // 0.0
(Math.log 0.0) handle Domain -> 1.0           // 1.0

((raise (Fail "nope")) handle Fail s -> s)    // "nope"
```
