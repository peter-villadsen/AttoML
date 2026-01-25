# Module System

Syntax:
- `structure Name = { let x = expr, let y = expr }`
- `signature Sig = { val x : int, val f : int -> int }`
- `structure Name : Sig = { ... }` checks that all `val`s exist; type annotations are parsed and used in type env for checking basic presence.
- `open Name` brings `x` into scope, in addition to `Name.x` qualified access.

Processing:
- Frontend parses module declarations and loads them into `ModuleSystem`.
- `ModuleSystem.InjectStructuresInto` infers and injects qualified names into the `TypeEnv`.
- Interpreter evaluates structures to `ModuleVal` instances, injects qualified names into the value environment, and applies `open`.

Key files:
- `src/AttoML.Core/Parsing/Syntax.cs`
- `src/AttoML.Core/Parsing/Parser.cs`
- `src/AttoML.Core/Modules/ModuleSystem.cs`
