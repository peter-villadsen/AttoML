# AttoML Examples

This directory contains example AttoML programs demonstrating various features.

## Running Examples

Run an example file:
```bash
dotnet run --project src\AttoML.Interpreter examples\symcalc_demo.atto
```

Run with verbose output to see type checking details:
```bash
dotnet run --project src\AttoML.Interpreter -- --verbose examples\symcalc_demo.atto
```

## Available Examples

### symcalc_demo.atto
Demonstrates the symbolic calculus module:
- Expression parsing
- Symbolic differentiation
- Expression simplification
- Pretty-printing expressions

Example output:
```
val it : string = "2.0 + 3.0 * 4.0"
val it : string = "3.0 * x ^ 2.0"
...
```

### complex_demo.atto
Demonstrates complex number arithmetic:
- Complex number addition
- Complex number multiplication
- Conjugation
- Magnitude calculations
- Polar coordinate conversion

## Interactive REPL

To experiment interactively:
```bash
dotnet run --project src\AttoML.Interpreter -- --repl
```

Then you can:
```ml
>> open SymCalc
>> parse "x^2 + 2*x + 1"
val it : Expr = Add(Add(Pow(Var("x"), Const(2)), Mul(Const(2), Var("x"))), Const(1))
>> toString it
val it : string = "x ^ 2.0 + 2.0 * x + 1.0"
>> diff ("x", it)
...
```

## Verbose Mode

Use `--verbose` (or `-v`) to see detailed information about:
- Prelude module loading
- Type inference
- Module injection
- Compilation steps

This is useful for debugging and understanding how the interpreter works.
