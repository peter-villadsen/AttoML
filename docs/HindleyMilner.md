# Hindley–Milner Type Inference

AttoML uses a simplified Hindley–Milner system:

- Types: `int`, `bool`, `float`, `string`, `unit`, function `t1 -> t2`, tuples `(t1, t2, ...)`, lists `[t]`, records `{x: t, ...}`, and algebraic data types.
- Type variables: `TVar` nodes represent unknown types.
- Schemes: `forall 'a1 'a2. t` quantifies polymorphic types for let-bound names.
- Constraint generation: during inference we recursively compute expression types and accumulate unification constraints.
- Unification: solves constraints, performing occurs-check and substitution composition.
- Generalization: let-bound expressions generalize free type variables to schemes.
- Instantiation: using a scheme produces fresh type variables.
 - Pattern matching: patterns contribute constraints and introduce bindings; branch result types are unified to a single expression type. Constructor patterns constrain scrutinee types to ADTs.

Key files:
- `src/AttoML.Core/Types/Types.cs`
- `src/AttoML.Core/Types/TypeEnv.cs`
- `src/AttoML.Core/Types/TypeInference.cs`
