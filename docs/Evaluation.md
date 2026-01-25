# Evaluation Model

- Values: integers, floats, strings, booleans, unit, tuples, closures, modules.
- Environments: `Env` maps names to values; cloning implements lexical scoping.
- Closures: `ClosureVal` captures an environment and is represented as `<fun>`.
- Let: evaluate binding, extend environment, evaluate body.
- Let rec: create a closure that refers to itself; extend environment with the closure.
- Qualified access: `Module.name` resolved via injected qualified names.
- Modules: structures evaluate all `let` bindings into a `ModuleVal`, then inject `Module.name` into global env; `open` brings members into scope.
 - Pattern matching: evaluate the scrutinee, then try patterns top-to-bottom; binds variables on success and evaluates the branch; errors on non-exhaustive matches at runtime.
 - Boolean short-circuit: `andthen`/`orelse` are parsed into `if then else` forms so the right-hand side only evaluates when needed.

## Exceptions

- Language-level exceptions are values of type `exn` (e.g., `<Div>`, `<Domain>`, `<Fail "msg">`).
- `raise e` throws a host-side `AttoException` that carries the `exn` value.
- `expr handle pat -> e1 | ...` is implemented by evaluating `expr` and catching `AttoException`; the payload `exn` value is pattern-matched against the handlers.
- We keep host errors (e.g., interpreter bugs or IO) as normal .NET exceptions; only language-level raises use `AttoException`.
- Operators and libraries can raise:
	- `/` on integers, `div`, and `mod` with zero divisor raise `Div`.
	- `Math.sqrt` for `x < 0` and `Math.log` for `x <= 0` raise `Domain`.

See also: `docs/Exceptions.md` for language surface and examples.

Key files:
- `src/AttoML.Interpreter/Runtime/Value.cs`
- `src/AttoML.Interpreter/Runtime/Env.cs`
- `src/AttoML.Interpreter/Runtime/Evaluator.cs`
- `src/AttoML.Interpreter/Runtime/AttoException.cs`
