# AttoML

AttoML is a small ML-like language implemented in C#. It has a shared frontend (lexer, parser, AST, Hindley–Milner type inference) used by an interpreter today, with a future compiler planned.

Features:
- Expressions: literals, variables, `fun x -> expr`, application `f x`, `let`/`let rec`, `if then else`, tuples
 - Types: int, bool, float, string, unit; function types; tuples; lists; records; algebraic types; Hindley–Milner inference
 - Types: int, bool, float, string, unit; function types; tuples; lists; records; algebraic data types (ADTs); Hindley–Milner inference (constraints + unification)
 - Pattern matching: `match expr with | pat -> expr | ...`, tuple and constructor patterns, type-checked and evaluated
 - Operators: arithmetic `+ - * /`, relational `< > <= >=`, structural equality `=` and inequality `<>`, short-circuit boolean `andthen`/`orelse`
 - Operators: arithmetic `+ - * /`, integer `div`/`mod`, relational `< > <= >=`, structural equality `=` and inequality `<>`, short-circuit boolean `andthen`/`orelse`
- Modules: `structure`, `signature`, `open`, and qualified access `Module.value`
 - ADTs: `type Option = Some of int | None`, constructors as values/closures
- Base and Math modules: arithmetic, booleans, comparisons; `Math.exp/log/sin/cos/sqrt/atan/atan2`
- Exceptions: `exception` declarations, `raise`, `handle`, built-ins `Div`, `Domain`, `Fail of string`
- REPL: prints value and inferred type, binds to `it`
 - Verbose mode: `-v/--verbose` prints the parsed AST
 - Verbose mode: `-v/--verbose` prints the parsed AST
 - Top-level bindings: `val x = expr` and annotated `val x : T = expr`; `it` updated after vals and expressions
 - Let annotations: `let x : T = expr in ...` with type-checked annotation
 - Function parameters can be tuple patterns: `fun (x, y) -> expr`
 - Function definition sugar:
	 - Top-level: `fun add x y = x + y` desugars to `val add = fun x -> fun y -> x + y`
	 - Tuple params: `fun addPair (x, y) = x + y` desugars via a match on the tuple

## Quick Start

Build and run:

```
 dotnet build
 dotnet run --project src/AttoML.Interpreter
```

Examples:

```
>> fun x -> x
val it : 'a0 -> 'a0 = <fun>
>> let x = 1 in x
val it : int = 1
>> Base.add 2 3
val it : int = 5
>> Math.sin 0.0
val it : float = 0
>> 1.5 + 2.25
val it : float = 3.75
>> [1, 2, 3]
val it : [int] = [1, 2, 3]
>> { x = 1, y = true }
val it : {x: int, y: bool} = {x = 1, y = true}
>> type Option = Some of int | None; Some 3
val it : Option = <Some 3>
>> match Some 3 with | Some x -> x | None -> 0
val it : int = 3
>> 1 + 2 * 3
val it : int = 7
>> 1.5 < 2.0
val it : bool = true
>> [1,2] @ [3]
val it : [int] = [1, 2, 3]
>> (true andthen (Base.fail "won't run")) orelse false
val it : bool = true
>> (1 / 0) handle Div -> 42
val it : int = 42
>> (Math.sqrt (0.0 - 1.0)) handle Domain -> 0.0
val it : float = 0
>> val x : int = 1
val x : int = 1
val it : int = 1
>> (fun (a, b) -> a + b) (2, 3)
val it : int = 5
>> fun add x y = x + y
val add : 'a0 -> 'a1 -> <fun>
>> add 1 2
val it : int = 3
>> fun addPair (x, y) = x + y
val addPair : ('a2 * 'a3) -> <fun>
>> addPair (1, 2)
val it : int = 3
```

Load a file:

```
 dotnet run --project src/AttoML.Interpreter path\to\file.atml
## Complex Numbers
AttoML ships a small prelude module for complex numbers written in the AttoML language. It is loaded automatically by the interpreter.

- Type: `type Complex = C of float * float`
- Module: `structure Complex = { add, sub, mul, conj, magnitude, magnitudeSquared, ofPolar, toPolar }`

Examples:

```
open Complex
val a = C (1.5, -2.25)
val b = C (3.0, 4.0)
val s = add a b
val p = mul a b
val m = magnitude a
val c = match toPolar b with (mag, theta) -> ofPolar mag theta
```

See samples/complex_demo.atto for a script version.

```

## Architecture
- Core (`AttoML.Core`): `Lexer`, `Parser`, AST (`Syntax`), types (`Types`/`TypeEnv`), HM inference (`TypeInference`), module processing (`ModuleSystem`), `Frontend` driver.
- Interpreter (`AttoML.Interpreter`): runtime `Value` and `Env`, `Evaluator`, built-ins (`Base`, `Math`), `Program` REPL.
- Tests: xUnit covering lexing, parsing, type inference, evaluation, modules.

## Docs
- docs/HindleyMilner.md: Type inference design
- docs/Evaluation.md: Evaluation model and environments
- docs/Modules.md: Module system semantics
- docs/Architecture.md: Overview and data structures
 - docs/Operators.md: Operator semantics, rewrites, and short-circuiting
 - docs/Exceptions.md: Exception semantics, built-ins, and AttoException rationale
 - docs/REPL.md: Multi-line input (blank line or ';;' ends block)

## CI
GitHub Actions workflow builds and tests on pushes and PRs.
