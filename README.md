# AttoML

AttoML is a small ML-like language implemented in C#. It has a shared frontend (lexer, parser, AST, Hindley–Milner type inference) used by an interpreter today, with a future compiler planned.

## What's New! 🎉

### Arbitrary Precision Integers (IntInf)
AttoML now supports **arbitrary precision integers** using Standard ML's IntInf interface! Work with numbers of unlimited size:

```ml
(* Literal syntax with I suffix *)
42I
999999999999999999999999999999999999999999I

(* All arithmetic operators work *)
10I + 20I * 3I
123456789I * 987654321I

(* Compute huge powers *)
IntInf.pow (2I, 100)  (* 2^100 = 1267650600228229401496703205376I *)

(* Pattern matching *)
match x with
  0I -> "zero"
| 42I -> "the answer"
| _ -> "other"
```

**Benefits**: No integer overflow, perfect for large computations, cryptography, combinatorics.

### Parametric Polymorphism
AttoML now supports **parametric types** for algebraic data types! Define generic types that work with any type parameter:

```ml
(* Define once, use with any type *)
datatype 'a option = None | Some of 'a
datatype ('a, 'b) either = Left of 'a | Right of 'b
datatype 'a tree = Leaf | Node of 'a * 'a tree * 'a tree

(* Polymorphic constructors *)
Some 42          (* int option *)
Some "hello"     (* string option *)
Left 3.14        (* (float, 'b) either *)
Right "error"    (* ('a, string) either *)
```

**Benefits**: Type-safe generics, zero code duplication, full type inference, no runtime overhead.

### Pipe Operator
The **pipe operator `|>`** enables elegant function chaining, making code more readable and eliminating nested parentheses:

```ml
(* Before: nested functions *)
List.foldl (fun a -> fun x -> a + x) 0 (List.filter (fun x -> x > 5) (List.map (fun x -> x * 2) [1, 2, 3, 4, 5]))

(* After: clean pipeline *)
[1, 2, 3, 4, 5]
|> List.map (fun x -> x * 2)
|> List.filter (fun x -> x > 5)
|> List.foldl (fun a -> fun x -> a + x) 0
(* Result: 24 *)
```

**Benefits**: Left-to-right data flow, better readability, F#-style function composition.

## Features

### Core Language
- **Literals**: int, float, **intinf** (arbitrary precision integers), string, bool, unit
- **Variables**: identifier lookup with lexical scoping
- **Functions**:
  - Anonymous: `fun x -> body` or `fn x => body` (SML-style)
  - Named: `fun add x y = x + y` (desugars to nested lambdas)
  - Tuple patterns: `fun (x, y) -> x + y`
- **Application**: `f x` with left-associativity and currying
- **Let bindings**: `let x = expr in body` with type annotations
- **Recursion**: `let rec f x = body in rest`
- **Conditionals**: `if cond then expr1 else expr2`
- **Comments**:
  - Block comments: `(* ... *)` with nesting support
  - Line comments: `// comment to end of line`

### Data Structures
- **Tuples**: `(1, "hello", true)`
- **Lists**: `[1, 2, 3]` with `@` concatenation
- **Records**: `{x = 1, y = true, z = "test"}`
- **Algebraic Data Types**:
  - Monomorphic: `datatype Color = Red | Green | Blue`
  - **Parametric (NEW!)**: `datatype 'a option = None | Some of 'a`
  - Multiple parameters: `datatype ('a, 'b) either = Left of 'a | Right of 'b`

### Parametric Types (NEW!)

AttoML now supports **parametric polymorphism** for algebraic data types, allowing you to define generic types that work with any type parameter:

**Single Type Parameter:**
```ml
(* Define a generic option type *)
datatype 'a option = None | Some of 'a

(* Works with any type! *)
let intOpt = Some 42 in              (* int option *)
let strOpt = Some "hello" in         (* string option *)
let boolOpt = Some true in           (* bool option *)
(intOpt, strOpt, boolOpt)
```

**Multiple Type Parameters:**
```ml
(* Define a type with two parameters *)
datatype ('a, 'b) either = Left of 'a | Right of 'b

(* Use with different type combinations *)
let x = Left 42 in                   (* (int, 'b) either *)
let y = Right "error" in             (* ('a, string) either *)
let z = Left 3.14 in                 (* (float, 'c) either *)
(x, y, z)
```

**Recursive Parametric Types:**
```ml
(* Generic binary tree *)
datatype 'a tree = Leaf | Node of 'a * 'a tree * 'a tree

(* Integer tree *)
let intTree = Node (5,
                    Node (3, Leaf, Leaf),
                    Node (7, Leaf, Leaf))
in intTree

(* String tree *)
let strTree = Node ("root",
                    Node ("left", Leaf, Leaf),
                    Node ("right", Leaf, Leaf))
in strTree
```

**Type Constructors:**
Parametric type constructors are **polymorphic** and automatically instantiated:
```ml
datatype 'a option = None | Some of 'a

(* Constructor types *)
Some : forall 'a. 'a -> 'a option
None : forall 'a. 'a option

(* Type inference instantiates correctly *)
Some 42        (* 'a = int *)
Some "hi"      (* 'a = string *)
None           (* 'a remains polymorphic *)
```

**Pattern Matching:**
Pattern matching works seamlessly with parametric types:
```ml
datatype 'a option = None | Some of 'a

fun getOr opt default = match opt with
    Some x -> x
  | None -> default

(* Works with any type *)
getOr (Some 42) 0           (* int -> int *)
getOr (Some "hi") "bye"     (* string -> string *)
getOr None 3.14             (* float -> float *)
```

**Benefits:**
- **Code reuse**: Write generic data structures once
- **Type safety**: Full type checking with inference
- **Zero runtime overhead**: Types erased after compilation
- **No code duplication**: One implementation for all types

### Pattern Matching
Full pattern matching with `match...with` syntax:
- **Primary syntax**: `match expr with pat1 -> e1 | pat2 -> e2`
- **With explicit end**: `match expr with ... end` (recommended for nested matches)
- **Backward compatible**: `case expr of ...` (legacy syntax, still supported)

**Pattern types:**
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

**Nested Matching:**
For deeply nested or complex match expressions, use the `end` keyword to avoid ambiguity:
```ml
match Some 42 with
  Some x ->
    match Some 10 with
      Some y -> x + y
    | None -> x
    end
| None -> 0
end
```

The `end` keyword is optional for simple cases but recommended for clarity in nested matches.

### Type System
- **Primitive types**: `int`, `bool`, `float`, `intinf`, `string`, `unit`
- **Compound types**: function types, tuples, lists, records, ADTs
- **Exception type**: `exn` with pattern matching support
- **Parametric polymorphism (NEW!)**: Type parameters in ADTs
  - Single parameter: `'a option`, `'a list`, `'a tree`
  - Multiple parameters: `('a, 'b) either`, `('k, 'v) map`
- **Hindley-Milner inference**: Full constraint-based type inference with:
  - Polymorphic let-bound names
  - Polymorphic ADT constructors: `Some : forall 'a. 'a -> 'a option`
  - Occurs check
  - Type annotations: `let x : int = 5 in ...`

### Operators
- **Arithmetic**: `+`, `-`, `*`, `/` (polymorphic over int/float)
- **Integer**: `div`, `mod` (raises `Div` exception on zero)
- **Relational**: `<`, `>`, `<=`, `>=` (polymorphic)
- **Equality**: `=` (structural), `<>` (inequality)
- **Boolean**: `andthen`/`andalso` (short-circuit AND), `orelse` (short-circuit OR)
- **List**: `@` (concatenation), `::` (cons in patterns and expressions)
- **Pipe**: `|>` (forward application for function chaining)

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

#### Native Modules (C#)
- **Base**: `add`, `sub`, `mul`, `div`, `eq`, `lt`, `and`, `or`, `not`
- **Math**: `pi`, `exp`, `log`, `sin`, `cos`, `asin`, `acos`, `atan`, `atan2`, `sinh`, `cosh`, `tanh`, `sqrt` (raises `Domain`)
- **IntInf** (NEW!): `fromInt`, `toInt`, `toString`, `fromString`, `add`, `sub`, `mul`, `div`, `mod`, `neg`, `abs`, `compare`, `min`, `max`, `pow`, `sign` - Arbitrary precision integers (Standard ML IntInf)
- **List**: `append`, `map`, `filter`, `foldl`, `foldr`, `head`, `tail`, `length`, `null`
- **String**: `concat`, `size`, `sub`, `substring`, `explode`, `implode`, `compare`
- **Tuple**: `fst`, `snd`, `swap`, `curry`, `uncurry`, `fst3`, `snd3`, `thd3`
- **Set**: `empty`, `singleton`, `add`, `remove`, `contains`, `size`, `isEmpty`, `union`, `intersect`, `diff`, `isSubset`, `toList`, `fromList`
- **Map**: `empty`, `singleton`, `add`, `remove`, `get`, `contains`, `size`, `isEmpty`, `keys`, `values`, `toList`, `fromList`, `mapValues`, `fold`
- **TextIO** (NEW!): `print`, `openIn`, `openOut`, `openAppend`, `closeIn`, `closeOut`, `input`, `inputLine`, `output`, `flushOut`, `stdIn`, `stdOut`, `stdErr` - Standard ML-style file and console I/O ([docs](docs/TextIO_and_HTTP.md))
- **Http** (NEW!): `get`, `post`, `postJson`, `getWithHeaders` - Basic HTTP client for web requests ([docs](docs/TextIO_and_HTTP.md))

#### Prelude Modules (AttoML)
- **Option** (polymorphic `'a option`): `isSome`, `isNone`, `getOr`, `map`, `bind`, `filter`, `fold`, `toList`, `fromList`, `map2`, `orElse`, `andThen`
- **Result**: `isOk`, `isError`, `getOr`, `getError`, `map`, `mapError`, `bind`, `andThen`, `orElse`, `fold`, `toOption`, `errorToOption`, `map2`, `andAlso`, `orElse2`
- **Complex**: Complex number arithmetic (`add`, `sub`, `mul`, `conj`, `magnitude`, `ofPolar`, `toPolar`)

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

(* Arbitrary precision integers *)
42I
IntInf.pow (2I, 100)

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

### IntInf (Arbitrary Precision Integers)
```
>> 42I
val it : intinf = 42I

>> 999999999999999999999999999999999999999999I
val it : intinf = 999999999999999999999999999999999999999999I

>> 10I + 20I
val it : intinf = 30I

>> 123456789I * 987654321I
val it : intinf = 121932631112635269I

>> IntInf.pow (2I, 100)
val it : intinf = 1267650600228229401496703205376I

>> IntInf.fromInt 12345
val it : intinf = 12345I

>> IntInf.toString 999999999999999999999999I
val it : string = "999999999999999999999999"

>> match 42I with 0I -> "zero" | 42I -> "the answer" | _ -> "other"
val it : string = "the answer"
```

### Math Module Examples
```
>> Math.pi
val it : float = 3.14159265358979

>> Math.sin (Math.pi / 2.0)
val it : float = 1

>> Math.asin 0.5
val it : float = 0.523598775598299

>> Math.acos 0.0
val it : float = 1.5707963267949

>> Math.sinh 1.0
val it : float = 1.17520119364380

>> Math.cosh 0.0
val it : float = 1

>> Math.tanh 0.5
val it : float = 0.46211715726001

>> let x = 2.0 in (Math.cosh x * Math.cosh x) - (Math.sinh x * Math.sinh x)
val it : float = 1
```

### Pipe Operator Examples
```
>> (* Simple chaining *)
>> 5 |> (fun x -> x + 1) |> (fun x -> x * 2)
val it : int = 12

>> (* List processing pipeline *)
>> [1, 2, 3, 4, 5]
   |> List.map (fun x -> x * 2)
   |> List.filter (fun x -> x > 5)
val it : [int] = [6, 8, 10]

>> (* Option chaining *)
>> Some 10
   |> Option.map (fun x -> x * 3)
   |> Option.filter (fun x -> x > 20)
   |> Option.getOr 0
val it : int = 30

>> (* Math pipeline *)
>> 16.0 |> Math.sqrt |> (fun x -> x + 1.0)
val it : float = 5

>> (* Partial application *)
>> let add x y = x + y in
   5 |> add 10
val it : int = 15
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

>> match [1, 2, 3, 4] with a::b::rest -> a + b
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

**Monomorphic ADTs:**
```
>> datatype Color = Red | Green | Blue
>> Red
val it : Color = <Red>

>> match Red with Red -> 1 | Green -> 2 | Blue -> 3
val it : int = 1
```

**Parametric ADTs (NEW!):**
```
>> datatype 'a option = None | Some of 'a
>> Some 42
val it : option = <Some 42>

>> Some "hello"
val it : option = <Some "hello">

>> match Some 42 with Some x -> x | None -> 0
val it : int = 42

>> datatype 'a list = Nil | Cons of 'a * 'a list
>> Cons (1, Cons (2, Nil))
val it : list = <Cons (1, <Cons (2, <Nil>)>)>

>> datatype ('a, 'b) either = Left of 'a | Right of 'b
>> Left 42
val it : either = <Left 42>

>> Right "error"
val it : either = <Right "error">
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

## Runtime Libraries

### Tuple Module

Provides operations on tuples (pairs and triples):

```ml
>> Tuple.fst (1, 2)
val it : int = 1

>> Tuple.snd (1, 2)
val it : int = 2

>> Tuple.swap (1, 2)
val it : (int * int) = (2, 1)

>> let addPair = fn (x, y) => x + y in
   let curriedAdd = Tuple.curry addPair in
   curriedAdd 3 5
val it : int = 8

>> Tuple.fst3 (1, 2, 3)
val it : int = 1
```

Functions: `fst`, `snd`, `swap`, `curry`, `uncurry`, `fst3`, `snd3`, `thd3`

### Option Module

Provides the **polymorphic** `'a option` type for representing optional values:

```ml
>> datatype 'a option = None | Some of 'a

(* Works with any type! *)
>> Option.isSome (Some 42)
val it : bool = true

>> Option.isSome (Some "hello")
val it : bool = true

>> Option.getOr None 99
val it : int = 99

>> Option.map (fn x => x * 2) (Some 21)
val it : option = <Some 42>

>> Option.map (fn s => s ^ "!") (Some "hello")
val it : option = <Some "hello!">

>> Option.filter (fn x => x > 10) (Some 42)
val it : option = <Some 42>

>> Option.filter (fn x => x > 50) (Some 42)
val it : option = <None>

>> Option.toList (Some 42)
val it : [int] = [42]

>> Option.fromList [1, 2, 3]
val it : option = <Some 1>

(* Fold with two functions: onSome and onNone *)
>> Option.fold (fn x => x * 3) (fn _ => 0) (Some 14)
val it : int = 42
```

**Type signature**: All functions work with `'a option` (polymorphic!)

Functions: `isSome`, `isNone`, `getOr`, `map`, `bind`, `filter`, `fold`, `toList`, `fromList`, `map2`, `orElse`, `andThen`

### Result Module

Provides the `Result` type for error handling with Ok/Error variants:

```ml
>> datatype Result = Ok of int | Error of int
>> Result.isOk (Ok 42)
val it : bool = true

>> Result.getOr (Error 1) 99
val it : int = 99

>> Result.map (fn x => x * 2) (Ok 21)
val it : Result = <Ok 42>

>> Result.mapError (fn e => e + 100) (Error 1)
val it : Result = <Error 101>

>> Result.bind (fn x => if x > 0 then Ok (x * 2) else Error 1) (Ok 21)
val it : Result = <Ok 42>

>> Result.fold (fn x => x + 10) (fn e => 0) (Ok 32)
val it : int = 42
```

Functions: `isOk`, `isError`, `getOr`, `getError`, `map`, `mapError`, `bind`, `andThen`, `orElse`, `fold`, `toOption`, `errorToOption`, `map2`, `andAlso`, `orElse2`

### Set Module

Provides polymorphic immutable sets with efficient operations (works with any type):

```ml
>> let s1 = Set.add 2 (Set.singleton 1) in
   let s2 = Set.add 3 (Set.add 2 Set.empty) in
   Set.union s1 s2
val it : Set = {1, 2, 3}

>> Set.contains 2 (Set.fromList [1, 2, 3])
val it : bool = true

>> let s1 = Set.fromList [1, 2, 3] in
   let s2 = Set.fromList [2, 3, 4] in
   Set.intersect s1 s2
val it : Set = {2, 3}

>> let s1 = Set.fromList [1, 2, 3] in
   let s2 = Set.fromList [2, 3] in
   Set.isSubset s2 s1
val it : bool = true

>> Set.size (Set.fromList [1, 2, 3, 2, 1])
val it : int = 3

>> Set.toList (Set.diff (Set.fromList [1, 2, 3]) (Set.fromList [2]))
val it : [int] = [1, 3]

(* Polymorphic: works with any type! *)
>> let strSet = Set.fromList ["hello", "world", "hello"] in
   Set.toList strSet
val it : [string] = ["hello", "world"]

>> Set.contains true (Set.fromList [true, false])
val it : bool = true
```

Functions: `empty`, `singleton`, `add`, `remove`, `contains`, `size`, `isEmpty`, `union`, `intersect`, `diff`, `isSubset`, `toList`, `fromList`

### Map Module

Provides polymorphic immutable maps/dictionaries (works with any key and value types):

```ml
>> let m = Map.add 2 20 (Map.singleton 1 10) in
   Map.get 1 m
val it : Option = <Some 10>

>> Map.get 3 (Map.singleton 1 10)
val it : Option = <None>

>> let m = Map.fromList [(1, 10), (2, 20), (3, 30)] in
   Map.size m
val it : int = 3

>> let m = Map.fromList [(1, 10), (2, 20)] in
   Map.keys m
val it : [int] = [1, 2]

>> let m = Map.fromList [(1, 10), (2, 20)] in
   Map.mapValues (fn x => x * 2) m
val it : Map = {1 -> 20, 2 -> 40}

>> let m = Map.fromList [(1, 10), (2, 20)] in
   Map.fold (fn k => fn v => fn acc => acc + v) 0 m
val it : int = 30

>> Map.toList (Map.remove 2 (Map.fromList [(1, 10), (2, 20), (3, 30)]))
val it : [(int * int)] = [(1, 10), (3, 30)]

(* Polymorphic: works with any key and value types! *)
>> let strMap = Map.fromList [("one", 1), ("two", 2), ("three", 3)] in
   Map.get "two" strMap
val it : Option = <Some 2>

>> Map.size (Map.fromList [(true, "yes"), (false, "no")])
val it : int = 2
```

Functions: `empty`, `singleton`, `add`, `remove`, `get`, `contains`, `size`, `isEmpty`, `keys`, `values`, `toList`, `fromList`, `mapValues`, `fold`

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

- **Tests**: xUnit test suite (321 tests, 316 passing) covering:
  - Lexing and parsing
  - Type inference and parametric polymorphism
  - Pattern matching
  - Evaluation
  - Modules and polymorphic types
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

### Core Language Features

#### ✅ Fully Supported
- **Algebraic data types** (`datatype` / `type`)
  - **Parametric types (NEW!)**: `'a option`, `('a, 'b) either`
  - Type parameters in constructors
  - Polymorphic constructor schemes
- **Pattern matching** (`case`/`of`, `match`/`with`)
  - Literal patterns, wildcards, variables
  - Tuple and record patterns
  - List patterns including cons (`::``)
  - Constructor patterns
  - Nested patterns
- **Anonymous functions** (`fn`/`=>`, `fun`/`->`)
- **Let bindings** with type annotations
- **Recursive functions** (`let rec`)
- **Exception handling** (`raise`, `handle`)
- **Module system**
  - Structures (`structure`)
  - Signatures (`signature`)
  - Signature checking
  - Open declarations
  - Qualified access
- **Hindley-Milner type inference** with polymorphism

#### ❌ Not Supported (vs Standard ML)
- **Functors** (parameterized modules)
- **Type abbreviations** (separate from ADTs)
- **`val rec`** (use `let rec` instead)
- **References and mutation** (`ref`, `:=`, `!`)
- **User-defined infix operators**
- **Lazy evaluation** and `lazy`
- **`abstype`** declarations

### Runtime Libraries

#### ✅ Available
AttoML provides a comprehensive set of functional programming libraries:

**Native Modules (C#):**
- **Base**: Core arithmetic and logical operations
- **Math**: Constants (pi), exponential/logarithmic (exp, log), trigonometric (sin, cos, asin, acos, atan, atan2), hyperbolic (sinh, cosh, tanh), and other functions (sqrt)
- **List**: Full suite of list operations (map, filter, fold, etc.)
- **String**: String manipulation and conversion
- **Tuple**: Tuple operations and currying utilities
- **Set**: Polymorphic immutable sets with set algebra operations (`'a set`)
- **Map**: Polymorphic immutable maps/dictionaries (`('k, 'v) map`)

**Prelude Modules (AttoML):**
- **Option**: Standard option type with 11 operations
- **Result**: Error handling with Ok/Error variants (15 operations)
- **Complex**: Complex number arithmetic
- **SymCalc**: Symbolic differentiation and simplification

#### 🔄 Partial (vs SML Basis Library)
- **Fully polymorphic collections**: Set and Map modules are fully polymorphic, working with any type (`'a set`, `('k, 'v) map`)
- **No Array module**: Mutable arrays not supported (pure functional focus)
- **TextIO**: Basic file I/O operations available (print, file read/write)
- **No BinIO**: Binary I/O not supported
- **No OS/Process**: No operating system interface
- **IntInf module**: Arbitrary precision integers with Standard ML IntInf interface
- **Limited String**: Core operations present, missing advanced features

### Design Philosophy
AttoML aims to be a **teaching language** that:
- **Captures the essence** of ML-style functional programming
- **Supports modern syntax** (both ML and OCaml-inspired styles)
- **Remains simple** and understandable for learners
- **Provides excellent type error messages** for education
- **Focuses on purity** (no mutation, no I/O)
- **Offers practical libraries** (Option, Result, Set, Map) for real programs
- **Enables exploration** of functional concepts (closures, ADTs, pattern matching)

### AttoML vs Standard ML

**What AttoML Adds:**
- Dual syntax support (ML-style `fn/case/of` + OCaml-style `fun/match/with`)
- Modern libraries (Option, Result) as first-class prelude modules
- Symbolic math capabilities (SymCalc, Complex)
- Polymorphic Set and Map collections (`'a set`, `('k, 'v) map`)

**What AttoML Omits (Intentionally):**
- Side effects and mutation (teaching focus on pure FP)
- Advanced I/O operations (basic TextIO available)
- Advanced module features (functors, abstype)

AttoML is ideal for teaching functional programming concepts, type inference, and building small interpreters or symbolic computation tools.

## License

This work is provided under the MIT License.
