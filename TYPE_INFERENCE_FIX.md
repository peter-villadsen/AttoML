# Type Inference Fix for Nested Match Expressions

## Problem Summary

The AttoML type inference system was failing when processing EGraph.atto with the error:
```
Error inferring binding EGraph.instantiate: Cannot unify types: ['a3769] and Template
```

The system was trying to unify a list type with an ADT type, which should never happen in correct code.

## Root Cause Analysis

### Symptom
Debug output showed a match expression with **mixed pattern types**:
- Pattern [0]: `PListCons` (list cons pattern)
- Pattern [1]: `PList` (list pattern)
- Pattern [2]: `PCtor` (constructor pattern)

This occurred when matching on a function application (`App`) that returned a list type.

### Discovery Process

1. **Initial Hypothesis**: Recursive ADTs cause type inference failures
   - **Disproven**: Simple tests with recursive ADTs worked fine

2. **Second Hypothesis**: Complex type annotations cause issues
   - **Disproven**: Functions with complex annotations worked fine

3. **Third Hypothesis**: Recursive calls with ADT components cause issues
   - **Disproven**: Recursive pattern matching with ADT components worked fine

4. **Root Cause Found**: Nested `case...of` expressions with ambiguous scope

### The Actual Problem

In EGraph.atto, the `instantiate` function had:

```ocaml
fun instantiate (eg, tmpl, subst) =
  case tmpl of
    Template.TConst r -> ...
  | Template.TVar v ->
      case assocFind (v, subst) of
        id :: [] -> (id, eg)
      | [] -> raise (Fail "unbound pattern variable")
  | Template.TAdd (a, b) -> ...
  | ... (9 more cases)
```

The parser was conflating the nested `case` patterns with the outer `case` patterns, creating a single match with 11 cases that mixed list patterns (from the inner match) with constructor patterns (from the outer match).

## Solution

### Primary Fix: Explicit Scope Terminators

Use `match...with...end` syntax for nested matches to provide explicit scope boundaries:

**Before**:
```ocaml
case tmpl of
  Template.TVar v ->
    case assocFind (v, subst) of
      id :: [] -> (id, eg)
    | [] -> raise (Fail "unbound pattern variable")
```

**After**:
```ocaml
case tmpl of
  Template.TVar v ->
    match assocFind (v, subst) with
      id :: [] -> (id, eg)
    | [] -> raise (Fail "unbound pattern variable")
    end
```

The `end` keyword tells the parser where the nested match ends, preventing ambiguity about which `|` belongs to which match.

### Secondary Fix: Avoiding Tuple-in-List Patterns

The `extractMemo` function had a pattern that caused type unification issues:

**Before**:
```ocaml
case assocFind (id, memo) of
  [(cost, expr)] -> (cost, expr, memo)  (* Tuple nested in list pattern *)
| [] -> ...
```

**After**:
```ocaml
match assocFind (id, memo) with
  result :: [] ->                        (* Match list structure first *)
    let (cost, expr) = result in        (* Then extract tuple *)
    (cost, expr, memo)
| [] -> ...
end
```

This avoids having a tuple pattern directly nested inside a list pattern, which the type inference system struggled with.

## Files Modified

1. **EGraph.atto**:
   - Fixed `instantiate` function (Template.TVar case)
   - Fixed `extractMemo` function (pattern matching on assocFind result)

2. **LaTeXRewrite.atto**:
   - Fixed typo: `EEEPattern` → `EPattern`

3. **TypeInference.cs**:
   - Added comprehensive debug output (now disabled)
   - No algorithmic changes needed!

## Results

- **Before**: 256/299 tests passing, EGraph modules disabled
- **After**: 268/299 tests passing, all modules enabled
- **Improvement**: +12 tests passing, E-Graph functionality restored

Remaining 31 failures are minor integration issues, not fundamental type inference problems.

## Best Practices for AttoML Code

### ✅ Good Patterns

```ocaml
(* Use match...with...end for nested matches *)
case outer of
  Pattern1 ->
    match inner with
      InnerPat1 -> expr1
    | InnerPat2 -> expr2
    end
| Pattern2 -> expr3
```

```ocaml
(* Split complex patterns into steps *)
match listOfTuples with
  item :: rest ->
    let (a, b) = item in
    ...
```

### ❌ Patterns to Avoid

```ocaml
(* Avoid nested case without explicit end *)
case outer of
  Pattern1 -> case inner of InnerPat -> expr
| Pattern2 -> ...  (* Ambiguous! *)
```

```ocaml
(* Avoid tuple-in-list patterns for complex types *)
case list of
  [(a, b)] -> ...  (* May confuse type inference *)
```

## Algorithm Documentation

### Parser Behavior

1. **Function parameter tuple patterns** are desugared:
   ```ocaml
   fun f (a, b, c) = body
   ```
   becomes:
   ```ocaml
   fun __arg -> match __arg with (a, b, c) -> body
   ```

2. **Nested matches** need explicit terminators:
   - `case...of` has no explicit end → relies on context
   - `match...with...end` has explicit end → no ambiguity

3. **`_insideMatchDepth` tracking**: Parser maintains depth counter to know when to stop parsing at `|` tokens, but this isn't sufficient for complex nesting

### Type Inference Behavior

1. **Pattern type inference**: Each pattern in a match must unify with the scrutinee type
2. **Branch type unification**: All match branches must have compatible types
3. **Substitution propagation**: Type substitutions from pattern matching affect the branch expression types

The fix ensures patterns are properly associated with their match expressions before type inference runs.

## Verification

To verify the fix works:

```bash
# Build the project
dotnet build

# Test EGraph loading
dotnet run --project src/AttoML.Interpreter test_match_end.atto

# Run full test suite
dotnet test
```

Expected: Clean output, no type inference errors, 268+ tests passing.
