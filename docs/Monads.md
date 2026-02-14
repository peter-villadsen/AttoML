# Monads in AttoML

## Table of Contents

1. [Introduction](#introduction)
2. [What is a Monad?](#what-is-a-monad)
3. [Parser Monad](#parser-monad)
4. [State Monad](#state-monad)
5. [Writer Monad](#writer-monad)
6. [Running the Examples](#running-the-examples)

## Introduction

Monads are a powerful abstraction for structuring computations. They provide a uniform way to sequence operations while handling effects like parsing, state management, or logging. AttoML includes three monads in its Prelude:

- **Parser**: For building composable parsers
- **State**: For threading state through computations
- **Writer**: For accumulating logs alongside results

## What is a Monad?

A monad is a design pattern that:

1. **Wraps values** in a computational context
2. **Sequences computations** through `bind` (also written as `>>=`)
3. **Lifts pure values** into the context with `pure` (also called `return`)

### Monad Laws

Every monad must satisfy three laws:

```ocaml
(* Left identity *)
bind (pure x) f  =  f x

(* Right identity *)
bind m pure  =  m

(* Associativity *)
bind (bind m f) g  =  bind m (fun x -> bind (f x) g)
```

### Core Operations

All monads provide:

```ocaml
(* Lift a pure value into the monad *)
pure : 'a -> 'm 'a

(* Sequence two monadic computations *)
bind : 'm 'a -> ('a -> 'm 'b) -> 'm 'b

(* Transform the result (derived from pure and bind) *)
map : ('a -> 'b) -> 'm 'a -> 'm 'b
```

## Parser Monad

### What is it?

The Parser monad represents computations that consume input strings and produce results. A parser has type:

```ocaml
datatype 'a Parser = Parser of (string -> ('a * string) option)
```

A parser is a function that:
- Takes an input string
- Returns `Some (result, remaining)` on success
- Returns `None` on failure

### Parser Combinators

Parser combinators are functions that combine small parsers into larger ones. This compositional approach makes complex parsers easy to build and understand.

#### Basic Combinators

```ocaml
(* Parse a specific character *)
char : char -> char Parser

(* Choose between two parsers *)
orElse : 'a Parser -> 'a Parser -> 'a Parser

(* Parse zero or more occurrences *)
many : 'a Parser -> 'a list Parser

(* Parse one or more occurrences *)
many1 : 'a Parser -> 'a list Parser

(* Parse items separated by a delimiter *)
sepBy : 'a Parser -> 'b Parser -> 'a list Parser

(* Parse between delimiters *)
between : 'a Parser -> 'b Parser -> 'c Parser -> 'c Parser
```

#### Example: Building a Number Parser

```ocaml
(* Parse a single digit *)
let digit = Parser.satisfy Char.isDigit in

(* Parse multiple digits *)
let digits = Parser.many1 digit in

(* Convert to number *)
let number = Parser.map (fun ds -> String.toInt (String.implode ds)) digits
```

### Why is it useful?

Parser combinators provide:

1. **Composability**: Build complex parsers from simple ones
2. **Modularity**: Reuse parser components
3. **Declarative**: Parsers read like grammar rules
4. **Type Safety**: The type system ensures parsers are well-formed

### Example Use Cases

- **Language parsers**: Parse programming languages, DSLs
- **Configuration files**: Parse JSON, YAML, INI files
- **Protocol parsers**: Parse network protocols, binary formats
- **Data extraction**: Extract structured data from text

### Complete Example

```ocaml
(* Parse key-value pairs like: name=Alice *)
let keyValue =
    Parser.bind Parser.identifier (fun key ->
        Parser.bind (Parser.char '=') (fun _ ->
            Parser.bind Parser.identifier (fun value ->
                Parser.pure (key, value)
            )
        )
    )
in
Parser.run keyValue "name=Alice"
(* Result: Some (("name", "Alice"), "") *)
```

See `examples/monad_parser_demo.atto` for more examples.

## State Monad

### What is it?

The State monad represents computations that thread state through a sequence of operations without mutation. A stateful computation has type:

```ocaml
datatype ('s, 'a) State = State of ('s -> ('a * 's))
```

A State computation is a function that:
- Takes an initial state
- Returns a result paired with the final state

### Core Operations

```ocaml
(* Get the current state *)
get : ('s, 's) State

(* Set the state *)
put : 's -> ('s, unit) State

(* Modify the state with a function *)
modify : ('s -> 's) -> ('s, unit) State

(* Get a component of the state *)
gets : ('s -> 'a) -> ('s, 'a) State
```

### Why is it useful?

The State monad provides:

1. **Immutability**: No mutable variables needed
2. **Composability**: Chain stateful operations declaratively
3. **Testability**: State transformations are pure functions
4. **Safety**: State changes are explicit and tracked by types

### Example Use Cases

- **Counters**: Thread a counter through computations
- **Random numbers**: Generate random values with a seed
- **Symbol tables**: Build compiler symbol tables
- **Game state**: Manage game state updates

### Complete Example

```ocaml
(* Generate 5 random numbers [0-99] *)
let fiveRandoms = Random.nextN 100 5 in
let numbers = State.eval fiveRandoms 12345 in
TextIO.print (String.concatList (List.map String.ofInt numbers))
(* Example output: [83, 42, 17, 96, 51] *)
```

### Random Number Generator

AttoML includes a Linear Congruential Generator using the State monad:

```ocaml
structure Random = {
    (* Generate random number in [0, n) *)
    fun next n =
        State.bind State.get (fun seed ->
            let newSeed = (seed * 1103515245 + 12345) mod 2147483648 in
            State.bind (State.put newSeed) (fun _ ->
                State.pure (newSeed mod n)
            )
        )
}
```

See `examples/monad_state_demo.atto` for more examples including:
- Stateful counters
- Random number generation
- Dice rolling simulation
- Fibonacci sequence generation

## Writer Monad

### What is it?

The Writer monad represents computations that accumulate output (logs) alongside their results. A Writer computation has type:

```ocaml
datatype 'a Writer = Writer of ('a * string list)
```

A Writer computation produces:
- A result value
- A list of log messages

### Core Operations

```ocaml
(* Append a log message *)
tell : string -> unit Writer

(* Append multiple log messages *)
tells : string list -> unit Writer

(* Run and capture logs *)
listen : 'a Writer -> (('a * string list), 'a) Writer

(* Transform the logs *)
censor : (string list -> string list) -> 'a Writer -> 'a Writer
```

### Why is it useful?

The Writer monad provides:

1. **Non-intrusive logging**: Logs don't clutter the main logic
2. **Composability**: Logs are automatically accumulated
3. **Separation of concerns**: Computation and logging are separate
4. **Testability**: Easy to verify what was logged

### Example Use Cases

- **Traced computations**: Track steps in an algorithm
- **Audit logs**: Record operations for compliance
- **Debugging**: Add detailed execution traces
- **Computation history**: Keep a record of decisions made

### Complete Example

```ocaml
(* Traced arithmetic *)
let computation =
    Writer.bind (Traced.add 10 5) (fun sum ->
        Writer.bind (Traced.multiply sum 2) (fun product ->
            Writer.bind (Traced.subtract product 3) (fun final ->
                Writer.pure final
            )
        )
    )
in
let (result, logs) = Writer.run computation in
(* Result: 27 *)
(* Logs: ["Adding 10 + 5 = 15\n",
          "Multiplying 15 * 2 = 30\n",
          "Subtracting 30 - 3 = 27\n"] *)
```

### Traced Arithmetic Module

AttoML includes traced arithmetic operations:

```ocaml
structure Traced = {
    fun add x y =
        trace ("Adding " ^ String.ofInt x ^ " + " ^
               String.ofInt y ^ " = " ^ String.ofInt (x + y) ^ "\n") (x + y)

    fun multiply x y =
        trace ("Multiplying " ^ String.ofInt x ^ " * " ^
               String.ofInt y ^ " = " ^ String.ofInt (x * y) ^ "\n") (x * y)
}
```

See `examples/monad_writer_demo.atto` for more examples including:
- Basic logging with `tell`
- Log filtering with `censor`
- Traced factorial computation
- Sequenced logged operations

## Running the Examples

All three monads come with comprehensive example files:

```bash
# Parser monad examples
dotnet run --project src/AttoML.Interpreter examples/monad_parser_demo.atto

# State monad examples
dotnet run --project src/AttoML.Interpreter examples/monad_state_demo.atto

# Writer monad examples
dotnet run --project src/AttoML.Interpreter examples/monad_writer_demo.atto
```

Each example file demonstrates:
- Basic operations
- Composition patterns
- Real-world use cases
- Best practices

## Monad Composition

Monads can be combined to handle multiple effects:

```ocaml
(* State + Writer: Stateful computation with logging *)
let statefulLogged = fun n ->
    State.bind State.get (fun count ->
        (* Log the operation *)
        let msg = "Count is: " ^ String.ofInt count ^ "\n" in
        State.bind (State.put (count + n)) (fun _ ->
            State.pure (count, msg)
        )
    )
```

## Further Reading

- **Monads in Functional Programming**: The classic paper "Monads for Functional Programming" by Philip Wadler
- **Parser Combinators**: "Monadic Parser Combinators" by Graham Hutton and Erik Meijer
- **Real World Haskell**: Chapter on Monads (concepts apply to ML)

## Summary

| Monad | Purpose | Type | Example |
|-------|---------|------|---------|
| **Parser** | Composable parsing | `string -> ('a * string) option` | Parse JSON, CSV, languages |
| **State** | Thread state | `'s -> ('a * 's)` | Random numbers, counters |
| **Writer** | Accumulate logs | `('a * string list)` | Traced arithmetic, audit logs |

All three monads follow the same pattern:
1. Wrap values in a computational context
2. Use `bind` to sequence operations
3. Use `pure` to lift values
4. Compose small operations into larger ones

This uniform interface makes monads powerful tools for structuring effectful computations in a pure functional language like AttoML.
