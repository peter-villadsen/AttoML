# AttoML

AttoML is a small ML-like language implemented in C#. It has a shared frontend (lexer, parser, AST, Hindley–Milner type inference) used by an interpreter today, with a future compiler planned.

## Features

### Core Language
- **Literals**: int, float, string, bool, unit
- **Variables**: identifier lookup with lexical scoping
- **Functions**:
  - Anonymous: `fun x -> body` or `fn x => body` (SML-style)
  - Named: `fun add x y = x + y` (desugars to nested lambdas)
  - Tuple patterns: `fun (x, y) -> x + y`
- **Application**: `f x` with left-associativity and currying
- **Let bindings**: `let x = expr in body` with type annotations
- **Recursion**: `let rec f x = body in rest`
- **Conditionals**: `if cond then expr1 else expr2`

### Data Structures
- **Tuples**: `(1, "hello", true)`
- **Lists**: `[1, 2, 3]` with `@` concatenation
- **Records**: `{x = 1, y = true, z = "test"}`
- **Algebraic Data Types**: `datatype Option = Some of int | None`

### Pattern Matching
Full pattern matching with `match expr with` or `case expr of` (SML-style):
- **Wildcard**: `_`
- **Variables**: `x`
- **Literals**: `42`, `3.14`, `"text"`, `true`, `()`
- **Tuples**: `(x, y, z)`
- **Lists**:
  - Empty: `[]`
  - Literal: `[1, 2, 3]`
  - **Cons**: `h::t` (NEW! destructure head and tail)
- **Records**: `{x = a, y = b}` (NEW! destructure records)
- **Constructors**: `Some x`, `None`, `Cons (h, t)`
- **Nested patterns**: All patterns compose recursively

### Type System
- **Primitive types**: `int`, `bool`, `float`, `string`, `unit`
- **Compound types**: function types, tuples, lists, records, ADTs
- **Exception type**: `exn` with pattern matching support
- **Hindley-Milner inference**: Full constraint-based type inference with:
  - Polymorphic let-bound names
  - Occurs check
  - Type annotations: `let x : int = 5 in ...`

### Operators
- **Arithmetic**: `+`, `-`, `*`, `/` (polymorphic over int/float)
- **Integer**: `div`, `mod` (raises `Div` exception on zero)
- **Relational**: `<`, `>`, `<=`, `>=` (polymorphic)
- **Equality**: `=` (structural), `<>` (inequality)
- **Boolean**: `andthen`/`andalso` (short-circuit AND), `orelse` (short-circuit OR)
- **List**: `@` (concatenation), `::` (cons in patterns)

### Modules
- **Structures**: `structure Name = { val x = 1, fun f x = x + 1 }`
- **Signatures**: `signature Sig = { val x : int, val f : int -> int }`
- **Signature checking**: `structure Name : Sig = { ... }`
- **Open**: `open Module` (brings members into scope)
- **Qualified access**: `Module.member`

### Exceptions
- **Declaration**: `exception MyExc of int`
- **Raising**: `raise (MyExc 42)`
- **Handling**: `expr handle MyExc n => result | Div => 0`
- **Built-ins**: `Div` (division by zero), `Domain` (invalid math), `Fail of string`

### Built-in Modules
- **Base**: `add`, `sub`, `mul`, `div`, `eq`, `lt`, `and`, `or`, `not`
- **Math**: `exp`, `log`, `sin`, `cos`, `sqrt`, `atan`, `atan2` (raises `Domain`)
- **List**: `append`, `map`, `filter`, `foldl`, `foldr`, `head`, `tail`, `length`, `null`
- **String**: `concat`, `size`, `sub`, `substring`, `explode`, `implode`, `compare`
- **Complex** (prelude): Complex number arithmetic written in AttoML

## SML Compatibility

AttoML now supports significant Standard ML syntax:

### SML-Style Syntax (NEW!)
```sml
(* Anonymous functions with fn/=> *)
fn x => x + 1

(* Pattern matching with case/of *)
case expr of
    Some x => x
  | None => 0

(* Boolean operators *)
true andalso false
true orelse false

(* List cons pattern *)
match [1, 2, 3] with
    h::t => h

(* Algebraic data types *)
datatype Tree = Leaf of int | Node of Tree * Tree
```

### Backward Compatible
All existing AttoML syntax still works:
```ml
fun x -> x + 1
match expr with Some x -> x | None -> 0
true andthen false
```

Both syntaxes can be mixed freely in the same program.

## Quick Start

Build and run:
```bash
dotnet build
dotnet run --project src/AttoML.Interpreter
```

## Examples

### Basic REPL Session
```
>> fn x => x
val it : 'a0 -> 'a0 = <fun>

>> let x = 1 in x
val it : int = 1

>> Base.add 2 3
val it : int = 5

>> Math.sin 0.0
val it : float = 0

>> 1.5 + 2.25
val it : float = 3.75
```

### Lists and Pattern Matching
```
>> [1, 2, 3]
val it : [int] = [1, 2, 3]

>> [1,2] @ [3,4]
val it : [int] = [1, 2, 3, 4]

>> match [1, 2, 3] with h::t => h
val it : int = 1

>> match [1, 2, 3] with h::t => t
val it : [int] = [2, 3]

>> case [1, 2, 3, 4] of a::b::rest => a + b
val it : int = 3

>> match [5, 10] with [a, b] => a + b | _ => 0
val it : int = 15
```

### Records
```
>> { x = 1, y = true }
val it : {x: int, y: bool} = {x = 1, y = true}

>> match {x = 10, y = 20} with {x = a, y = b} => a + b
val it : int = 30

>> let point = {x = 5, y = 12} in
   match point with {x = px, y = py} => Math.sqrt ((px * px) + (py * py))
val it : float = 13
```

### Algebraic Data Types
```
>> datatype Option = Some of int | None
>> Some 3
val it : Option = <Some 3>

>> case Some 42 of Some x => x | None => 0
val it : int = 42

>> datatype List = Cons of int * List | Nil
>> Cons (1, Cons (2, Nil))
val it : List = <Cons (1, <Cons (2, <Nil>)>)>
```

### Functions and Operators
```
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

>> 1 + 2 * 3
val it : int = 7

>> 1.5 < 2.0
val it : bool = true

>> true andalso false
val it : bool = false

>> (true andthen (Base.fail "won't run")) orelse false
val it : bool = true
```

### Exception Handling
```
>> (1 div 0) handle Div => 42
val it : int = 42

>> (Math.sqrt (0.0 - 1.0)) handle Domain => 0.0
val it : float = 0

>> exception MyExc of string
>> (raise (MyExc "error")) handle MyExc msg => 0
val it : int = 0
```

### Type Annotations
```
>> val x : int = 1
val x : int = 1
val it : int = 1

>> let y : float = 3.14 in y
val it : float = 3.14

>> fun typed (x : int) = x + 1
(* Note: parameter type annotations in progress *)
```

### Complex Patterns
```
>> match [{x = 1}, {x = 2}] with [{x = a}, {x = b}] => a + b | _ => 0
val it : int = 3

>> match {data = [1, 2, 3]} with {data = h::t} => h
val it : int = 1

>> match [[1, 2], [3, 4]] with [[a, b], [c, d]] => a + b + c + d | _ => 0
val it : int = 10
```

## Load Files

Run AttoML files:
```bash
dotnet run --project src/AttoML.Interpreter path/to/file.atto
```

## Complex Numbers (Prelude)

AttoML ships with a complex numbers module written in AttoML itself, loaded automatically:

```ml
open Complex

val a = C (1.5, -2.25)
val b = C (3.0, 4.0)

val s = add a b
val p = mul a b
val m = magnitude a

match toPolar b with (mag, theta) => ofPolar mag theta
```

Type: `datatype Complex = C of float * float`

Operations: `add`, `sub`, `mul`, `conj`, `magnitude`, `magnitudeSquared`, `ofPolar`, `toPolar`

See `samples/complex_demo.atto` for examples.

## Architecture

- **Core** (`AttoML.Core`):
  - `Lexer` - Tokenization with keyword recognition
  - `Parser` - Recursive descent parser with operator precedence
  - `Syntax` - AST node definitions
  - `Types`/`TypeEnv` - Type representations and environments
  - `TypeInference` - Hindley-Milner constraint-based inference
  - `ModuleSystem` - Module loading and signature checking
  - `Frontend` - Compilation driver

- **Interpreter** (`AttoML.Interpreter`):
  - `Value` and `Env` - Runtime values and environments
  - `Evaluator` - Tree-walking interpreter with closures
  - Built-ins: `Base`, `Math`, `List`, `String`
  - `Program` - REPL and file execution

- **Tests**: xUnit test suite (146 tests) covering:
  - Lexing and parsing
  - Type inference
  - Pattern matching
  - Evaluation
  - Modules
  - SML compatibility

## Documentation

- `docs/HindleyMilner.md` - Type inference design
- `docs/Evaluation.md` - Evaluation model and environments
- `docs/Modules.md` - Module system semantics
- `docs/Architecture.md` - Overview and data structures
- `docs/Operators.md` - Operator semantics and rewrites
- `docs/Exceptions.md` - Exception handling
- `docs/REPL.md` - Multi-line input (blank line or `;;` ends block)

## Development

Build:
```bash
dotnet build
```

Run tests:
```bash
dotnet test
```

Run REPL with verbose mode:
```bash
dotnet run --project src/AttoML.Interpreter -- -v
```

## CI

GitHub Actions workflow builds and tests on pushes and PRs.

## Language Comparison

### SML Features Supported
✅ Algebraic data types (`datatype`)
✅ Pattern matching (`case`/`of`)
✅ Anonymous functions (`fn`/`=>`)
✅ List cons pattern (`::`)`
✅ Record patterns
✅ Exception handling
✅ Module system (structures, signatures)
✅ Hindley-Milner type inference

### SML Features Not Yet Supported
❌ Functors (parameterized modules)
❌ Type abbreviations (separate from ADTs)
❌ `val rec` (use `let rec` instead)
❌ References and mutation (`ref`, `:=`, `!`)
❌ User-defined infix operators
❌ Full SML Basis Library

### Design Philosophy
AttoML aims to be a **teaching language** that:
- Captures the essence of ML-style functional programming
- Supports modern syntax (both ML and OCaml-inspired)
- Remains simple and understandable
- Provides excellent type error messages
- Focuses on pure functional programming

## License

[Add your license here]
