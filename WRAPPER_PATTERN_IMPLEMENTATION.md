# Wrapper Pattern Implementation

## Overview

Refactored TextIO and Map modules to use the **wrapper pattern**, solving the type system limitation with option types returned from C# builtins.

## Problem Solved

**Before**: C# builtins returning `AdtVal("Some"/"None")` couldn't be pattern matched:
```ocaml
let line = TextIO.inputLine stream in
match line with
  Some l -> l    (* ERROR: ADT mismatch *)
| None -> "EOF"
end
```

**After**: Pattern matching works perfectly:
```ocaml
let line = TextIO.inputLine stream in
match line with
  Some l -> l    (* ✓ Works! *)
| None -> "EOF"
end
```

## Architecture

### Two-Layer Design

**Layer 1: C# Implementation (low-level)**
- Returns simple types (lists, tuples, primitives)
- No ADT construction that conflicts with Prelude types
- Example: `inputLine` returns `string list` where `[]` = EOF, `[line]` = data

**Layer 2: AttoML Wrapper (high-level)**
- Wraps implementation to provide idiomatic ML API
- Constructs option values in AttoML code
- Pattern matching works because options are created in the same context as the Prelude

## What Changed

### TextIO Module

#### 1. Created TextIOImplementationModule (C#)
**File**: `src/AttoML.Interpreter/Builtins/TextIOImplementationModule.cs`

- Renamed from `TextIOModule`
- Changed `inputLine` to return `ListVal`:
  ```csharp
  // Before
  return new AdtVal("Some", new StringVal(line));

  // After
  return new ListVal(new[] { new StringVal(line) }); // [line]
  return new ListVal(Array.Empty<Value>());          // []
  ```

#### 2. Created TextIO Wrapper (AttoML)
**File**: `src/AttoML.Interpreter/Prelude/TextIO.atto`

```ocaml
structure TextIO = {
    (* Re-export simple functions *)
    val print = TextIOImplementation.print,
    val openIn = TextIOImplementation.openIn,
    (* ... etc ... *)

    (* Wrap inputLine to convert list to option *)
    fun inputLine stream =
        case TextIOImplementation.inputLine stream of
            [] -> None
          | line :: [] -> Some line
          | _ -> raise (Fail "impossible")
}
```

#### 3. Updated Type Signatures
**File**: `src/AttoML.Core/Frontend.cs`

```csharp
// Implementation layer (list return)
BaseTypeEnv.Add("TextIOImplementation.inputLine",
    new Types.TFun(instreamT, new Types.TList(strT)));

// Wrapper layer (option return)
var strOptionT = new Types.TAdt("option", new[] { strT });
BaseTypeEnv.Add("TextIO.inputLine",
    new Types.TFun(instreamT, strOptionT));
```

#### 4. Updated Registration
**File**: `src/AttoML.Interpreter/Program.cs`

```csharp
// Register implementation (internal)
var textIOImplMod = TextIOImplementationModule.Build();
evaluator.Modules["TextIOImplementation"] = textIOImplMod;
// ... register members ...

// Load wrapper from Prelude
LoadOne("TextIO.atto");
```

### Map Module

#### 1. Created MapImplementationModule (C#)
**File**: `src/AttoML.Interpreter/Builtins/MapImplementationModule.cs`

- Renamed from `MapModule`
- Changed `get` to return `ListVal`:
  ```csharp
  // Before
  return new AdtVal("Some", val);
  return new AdtVal("None", null);

  // After
  return new ListVal(new[] { val });     // [value]
  return new ListVal(Array.Empty<Value>()); // []
  ```

#### 2. Created Map Wrapper (AttoML)
**File**: `src/AttoML.Interpreter/Prelude/Map.atto`

```ocaml
structure Map = {
    (* Re-export functions that don't return options *)
    val empty = MapImplementation.empty,
    val add = MapImplementation.add,
    (* ... etc ... *)

    (* Wrap get to convert list to option *)
    fun get key map =
        case MapImplementation.get key map of
            [] -> None
          | value :: [] -> Some value
          | _ -> raise (Fail "impossible")
}
```

#### 3. Updated Type Signatures & Registration
Same pattern as TextIO

### Test Infrastructure

#### Updated Test Base
**File**: `tests/AttoML.Tests/AttoMLTestBase.cs`

```csharp
// Use implementation modules
var mapImplMod = MapImplementationModule.Build();
evaluator.Modules["MapImplementation"] = mapImplMod;

// Load wrappers from Prelude
LoadPreludeFile("TextIO.atto");
LoadPreludeFile("Map.atto");
```

## Benefits

### ✅ Pattern Matching Works
Users get idiomatic ML with working pattern matching:
```ocaml
match TextIO.inputLine stream with
  Some line -> process line
| None -> handleEOF ()
end
```

### ✅ Type Safety
- No type system workarounds needed
- Options created in AttoML = same type as Prelude
- Full Hindley-Milner inference works correctly

### ✅ Clean Separation
- C#: Low-level I/O, system calls, resource management
- AttoML: High-level API, ML idioms, convenience functions

### ✅ Extensibility
Easy to add ML-level conveniences without touching C#:
```ocaml
(* Add to TextIO structure *)
fun inputLines stream =
    let fun loop acc =
        case inputLine stream of
            None -> List.rev acc
          | Some line -> loop (line :: acc)
    in loop [] end
```

### ✅ Standard Pattern
Follows established functional programming library design:
- Standard ML Basis Library uses similar layering
- OCaml standard library wraps low-level primitives
- Haskell base library provides high-level wrappers

## Files Changed

### Created:
1. `src/AttoML.Interpreter/Builtins/TextIOImplementationModule.cs`
2. `src/AttoML.Interpreter/Builtins/MapImplementationModule.cs`
3. `src/AttoML.Interpreter/Prelude/TextIO.atto`
4. `src/AttoML.Interpreter/Prelude/Map.atto`
5. `WRAPPER_PATTERN_IMPLEMENTATION.md` (this file)

### Removed:
1. `src/AttoML.Interpreter/Builtins/TextIOModule.cs` (replaced)
2. `src/AttoML.Interpreter/Builtins/MapModule.cs` (replaced)

### Modified:
1. `src/AttoML.Core/Frontend.cs` - Type signatures for both layers
2. `src/AttoML.Interpreter/Program.cs` - Module registration and Prelude loading
3. `tests/AttoML.Tests/AttoMLTestBase.cs` - Use implementation modules + load wrappers
4. `docs/TextIO_and_HTTP.md` - Removed pattern matching limitation

## Test Results

✅ **All 306 tests pass**
✅ **Pattern matching on `TextIO.inputLine` works**
✅ **Pattern matching on `Map.get` works**
✅ **All examples work correctly**
✅ **No regressions**

## Lessons for Future Module Development

When implementing new modules that need to return ADT types:

### ✅ DO: Use the Wrapper Pattern
1. Create `ModuleImplementation` in C# with simple return types
2. Create `Module.atto` in Prelude with idiomatic API
3. Register implementation, load wrapper from Prelude

### ✅ DO: Use List Encoding
For optional returns:
- `[]` = None
- `[value]` = Some value
- Pattern matching works immediately

### ✅ DO: Use Tuple Encoding
For result types:
- `(true, value)` = success
- `(false, error)` = failure
- Clear semantics, works with pattern matching

### ❌ DON'T: Return ADTs from C# Builtins
Avoid creating `AdtVal("Some"/"None")` in C# code - it conflicts with Prelude types

### ❌ DON'T: Expose Implementation Modules
Keep `*Implementation` modules internal:
- Don't document them publicly
- Users should only see the high-level wrapper API

## Performance Impact

Minimal overhead:
- One extra function call per operation
- Negligible compared to I/O or map operations
- Pattern matching simplicity benefits outweigh cost

## Conclusion

The wrapper pattern successfully solves the ADT type system limitation, providing users with idiomatic ML APIs while maintaining clean separation between C# implementation and AttoML conveniences.

This pattern should be used for all future modules that need to return option, result, or other ADT types.
