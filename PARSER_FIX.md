# Parser Bug Fix - ADT Constructor Consumption

## Issue
Tests were failing because bare ADT constructor applications were evaluating to just their payload:
```ocaml
datatype MyType = Constructor of int
Constructor 42  (* Was evaluating to: int = 42 *)
                (* Should evaluate to: MyType = <Constructor 42> *)
```

## Root Cause
In `Parser.cs` `ParseTypeAtom()` (lines 1112-1129), after parsing a type like `int`, the parser entered a greedy while loop that consumed ALL subsequent identifiers as type constructors:

```csharp
while (Kind == TokenKind.Identifier) {
    var ctorName = Peek().Text;
    // Check stop keywords...
    Next(); // consume the type constructor
    baseType = new TypeApp(baseType, ctorName);
}
```

**Problem**: When parsing `datatype myoption = MYNONE | MYSOME of int` followed by `MYSOME 3`:
1. Parser parses `of int` → `TypeName("int")`
2. Sees next token is `MYSOME` (identifier from next line)
3. Consumes it as a type constructor: `TypeApp(int, "MYSOME")`  
4. Only `3` remains as the expression!
5. AST becomes just `IntLit(3)` instead of `App(Var("MYSOME"), IntLit(3))`

## Fix
Added check to stop at uppercase identifiers (data constructors):

```csharp
// CRITICAL FIX: Stop at uppercase identifiers (data constructors, not type constructors)
// Type constructors are lowercase (list, option, map), data constructors are uppercase (Some, None, MYSOME)
if (ctorName.Length > 0 && char.IsUpper(ctorName[0])) {
    break;
}
```

**Reasoning**: In ML, type constructors are lowercase (`list`, `option`, `map`, `set`) while data constructors are uppercase (`Some`, `None`, `MYSOME`, `Constructor`). This convention prevents the parser from consuming data constructors that belong to subsequent expressions.

## Files Modified
- `src/AttoML.Core/Parsing/Parser.cs` (lines 1120-1125)

## Test Results
- **Before**: 304/306 tests passing (2 ADT-related failures)
- **After**: **306/306 tests passing** ✅ (100% success rate!)

## Impact
This fix ensures that:
- ✅ Bare ADT constructor applications work correctly
- ✅ Type parsing respects ML naming conventions  
- ✅ Multi-line datatype declarations don't consume subsequent expressions
- ✅ All existing tests continue to pass

## Example
```ocaml
datatype MyType = Constructor of int
Constructor 42
```
**Now correctly evaluates to**: `MyType = <Constructor 42>` ✅
