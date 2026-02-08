# Implementing Parametric Types in AttoML

## Overview

This document outlines what's required to implement parametric polymorphism for ADTs, enabling syntax like:
```ocaml
datatype 'a option = NONE | SOME of 'a
datatype ('a, 'b) either = Left of 'a | Right of 'b
```

## Current State

### What Already Exists ✅

1. **Type Representation** (`Types.cs`):
   - `TAdt` class already has `TypeArgs` field: `IReadOnlyList<Type> TypeArgs`
   - Can represent `option<int>`, `either<string, bool>`, etc.
   - Infrastructure for type substitution exists

2. **Type Variables**:
   - `TVar` class exists for type variables
   - Used extensively in type inference
   - Fresh type variable generation works

3. **Scheme** (Polymorphic Types):
   - `Scheme` class can quantify over type variables: `forall 'a 'b. 'a -> 'b`
   - Used for polymorphic function types

### What's Missing ❌

1. **Parser**: Cannot parse type parameters in datatype declarations
2. **Type Declaration AST**: `TypeDecl` doesn't store type parameters
3. **Type Inference**: Doesn't instantiate parametric ADT constructors
4. **Constructor Types**: Constructor types are monomorphic

---

## Required Changes

### 1. AST Changes (Syntax.cs)

**Current**:
```csharp
public sealed class TypeDecl : ModuleDecl {
    public string Name;
    public List<TypeCtorDecl> Ctors;
    // ...
}
```

**Needed**:
```csharp
public sealed class TypeDecl : ModuleDecl {
    public string Name;
    public List<string> TypeParams;  // NEW: ['a', 'b', ...]
    public List<TypeCtorDecl> Ctors;

    public TypeDecl(string n, List<string> typeParams, List<TypeCtorDecl> ctors) {
        Name = n;
        TypeParams = typeParams;  // NEW
        Ctors = ctors;
    }
}
```

**Impact**:
- Low risk - just adds a field
- Backward compatible if `TypeParams` defaults to empty list

---

### 2. Parser Changes (Parser.cs)

**Current** (`ParseTypeDecl` at line 987):
```csharp
private TypeDecl ParseTypeDecl()
{
    Expect(TokenKind.Type);
    var name = Expect(TokenKind.Identifier).Text;
    Expect(TokenKind.Equals);
    // ... parse constructors
    return new TypeDecl(name, ctors);
}
```

**Needed**:
```csharp
private TypeDecl ParseTypeDecl()
{
    Expect(TokenKind.Type);

    // Parse optional type parameters: 'a or ('a, 'b)
    var typeParams = new List<string>();
    if (Kind == TokenKind.Quote)  // Single param: 'a option
    {
        Next();  // consume '
        typeParams.Add(Expect(TokenKind.Identifier).Text);
    }
    else if (Kind == TokenKind.LParen)  // Multiple params: ('a, 'b) either
    {
        Next();  // consume (
        while (true)
        {
            Expect(TokenKind.Quote);  // '
            typeParams.Add(Expect(TokenKind.Identifier).Text);
            if (!Match(TokenKind.Comma)) break;
        }
        Expect(TokenKind.RParen);
    }

    var name = Expect(TokenKind.Identifier).Text;
    Expect(TokenKind.Equals);
    // ... parse constructors
    return new TypeDecl(name, typeParams, ctors);
}
```

**Syntax to Support**:
```ocaml
(* Single parameter *)
datatype 'a option = NONE | SOME of 'a

(* Multiple parameters *)
datatype ('a, 'b) either = Left of 'a | Right of 'b
datatype ('k, 'v) map = Empty | Node of 'k * 'v * ('k, 'v) map

(* No parameters (backward compat) *)
datatype color = Red | Green | Blue
```

**Impact**:
- Medium risk - parser changes can break existing code
- Need comprehensive tests for edge cases
- Must handle both `'a` and `('a, 'b)` syntax

---

### 3. Type Expression Parsing

**Issue**: Type expressions like `'a`, `'b` need to be parsed in constructor payloads.

**Current** (`TypeFromTypeExpr` in ModuleSystem.cs, line 267):
```csharp
public static TypeT TypeFromTypeExpr(TypeExpr te)
{
    return te switch
    {
        TypeName tn => tn.Name switch
        {
            "int" => TConst.Int,
            "bool" => TConst.Bool,
            // ...
            _ => new TAdt(tn.Name)  // Treats unknowns as ADTs
        },
        // ...
    };
}
```

**Problem**: Type variables `'a` are parsed as `TypeName` with name `"a"`, not as type variables.

**Needed**:
1. Parser must recognize `'a` as a type variable, not a type name
2. New AST node: `TypeVar` in `Syntax.cs`:
   ```csharp
   public sealed class TypeVar : TypeExpr {
       public string Name;
       public TypeVar(string n) { Name = n; }
   }
   ```
3. Update `ParseTypeExpr()` to handle `Quote` token:
   ```csharp
   if (Kind == TokenKind.Quote)
   {
       Next();
       var varName = Expect(TokenKind.Identifier).Text;
       return new TypeVar(varName);
   }
   ```
4. Update `TypeFromTypeExpr` with type parameter context:
   ```csharp
   public static TypeT TypeFromTypeExpr(TypeExpr te, Dictionary<string, TVar> typeParams)
   {
       return te switch
       {
           TypeVar tv => typeParams.TryGetValue(tv.Name, out var tvar)
               ? tvar
               : throw new Exception($"Unbound type variable '{tv.Name}"),
           // ...
       };
   }
   ```

**Impact**:
- Medium-high risk - affects all type expression parsing
- Need to thread type parameter context through many functions

---

### 4. Module System Changes (ModuleSystem.cs)

**Current** (line 59-65):
```csharp
case AttoML.Core.Parsing.TypeDecl td:
    var ctors = new List<(string, TypeT?)>();
    foreach (var c in td.Ctors)
    {
        ctors.Add((c.Name, c.PayloadType == null ? null : TypeFromTypeExpr(c.PayloadType)));
    }
    Adts[td.Name] = (td.Name, ctors);
    break;
```

**Needed**:
```csharp
case AttoML.Core.Parsing.TypeDecl td:
    // Create fresh type variables for parameters
    var typeParamMap = new Dictionary<string, TVar>();
    foreach (var tpName in td.TypeParams)
    {
        typeParamMap[tpName] = new TVar();
    }

    // Parse constructor payloads with type parameter context
    var ctors = new List<(string, TypeT?)>();
    foreach (var c in td.Ctors)
    {
        ctors.Add((c.Name, c.PayloadType == null ? null : TypeFromTypeExpr(c.PayloadType, typeParamMap)));
    }

    // Store type parameters with ADT definition
    Adts[td.Name] = (td.Name, td.TypeParams.Select(tp => typeParamMap[tp]).ToList(), ctors);
    break;
```

**Storage Structure Change**:
```csharp
// OLD:
public Dictionary<string, (string TypeName, List<(string Ctor, TypeT? Payload)> Ctors)> Adts { get; }

// NEW:
public Dictionary<string, (string TypeName, List<TVar> TypeParams, List<(string Ctor, TypeT? Payload)> Ctors)> Adts { get; }
```

**Impact**:
- High risk - changes core data structure
- All ADT processing code must be updated

---

### 5. Constructor Type Generation (ModuleSystem.cs)

**Current** (line 227-234):
```csharp
foreach (var adt in Adts.Values)
{
    foreach (var (ctor, payload) in adt.Ctors)
    {
        TypeT ctorType = payload == null
            ? new TAdt(adt.TypeName)
            : new TFun(payload, new TAdt(adt.TypeName));
        var scheme = new Scheme(Array.Empty<TVar>(), ctorType);
        e.Add(ctor, scheme);
    }
}
```

**Problem**: Constructor types are **monomorphic** - `SOME : int -> option` instead of `SOME : 'a -> 'a option`.

**Needed**:
```csharp
foreach (var adt in Adts.Values)
{
    foreach (var (ctor, payload) in adt.Ctors)
    {
        // Build parametric ADT type: option<'a>, either<'a, 'b>, etc.
        var adtType = new TAdt(adt.TypeName, adt.TypeParams);

        // Constructor type: payload -> ADT or just ADT
        TypeT ctorType = payload == null ? adtType : new TFun(payload, adtType);

        // Quantify over type parameters to make polymorphic
        var scheme = new Scheme(adt.TypeParams, ctorType);

        e.Add(ctor, scheme);
    }
}
```

**Example**:
```ocaml
(* Declared: *)
datatype 'a option = NONE | SOME of 'a

(* Constructor types: *)
NONE : forall 'a. 'a option
SOME : forall 'a. 'a -> 'a option

(* Usage: *)
SOME 42       (* 'a instantiated to int -> int option *)
SOME "hello"  (* 'a instantiated to string -> string option *)
NONE          (* 'a remains polymorphic -> 'a option *)
```

**Impact**:
- High risk - changes how constructors are typed
- Affects pattern matching type inference

---

### 6. Type Inference: Pattern Matching

**Current**: Pattern matching on ADT constructors works, but types are monomorphic.

**Issue**: When matching `SOME x`, need to instantiate the polymorphic constructor type:
```ocaml
case opt of
    NONE -> 0
  | SOME x -> x + 1
```

Type inference must:
1. Get `SOME`'s scheme: `forall 'a. 'a -> 'a option`
2. Instantiate `'a` with fresh type variable: `'t1 -> 't1 option`
3. Unify scrutinee type with `'t1 option`
4. Bind `x : 't1`
5. Infer `x + 1`, unifying `'t1` with `int`

**Current Code** (`TypeInference.cs` - pattern matching):
Likely doesn't handle polymorphic constructors properly.

**Needed**:
- When encountering `PCtor` pattern, instantiate constructor's scheme
- Fresh type variables for each use

**Impact**:
- Very high risk - core type inference algorithm
- Need to handle nested patterns: `SOME (Left x)`
- Must preserve principal types

---

### 7. Type Inference: Constructor Applications

**Issue**: When using `SOME 42`, need to instantiate and unify.

**Needed**:
```ocaml
let x = SOME 42 in ...
(* Inference:
   1. SOME : forall 'a. 'a -> 'a option
   2. Instantiate: 't1 -> 't1 option
   3. Apply to 42 : int
   4. Unify 't1 ~ int
   5. Result: int option
*)
```

Type inference for constructor applications already works for monomorphic types. For polymorphic:
- Constructor lookup returns `Scheme` with quantifiers
- Must call `Instantiate(scheme)` to get fresh type variables
- Then proceed with normal application inference

**Impact**:
- Medium risk - may already work if schemes are stored correctly
- Need to verify instantiation happens

---

### 8. Type Unification: Parametric ADTs

**Current**: Unification handles `TAdt`, but assumes no type arguments.

**Needed**: Unify `option<int>` with `option<'a>`:
- Structurally: ADT names must match
- Recursively: Type arguments must unify pairwise

**Example**:
```ocaml
Unify(TAdt("option", [TConst.Int]), TAdt("option", [TVar(5)]))
  => Unify(TConst.Int, TVar(5))
  => Subst[5 -> int]

Unify(TAdt("either", [TConst.Int, TConst.Bool]), TAdt("either", [TVar(3), TVar(4)]))
  => Unify(TConst.Int, TVar(3)) + Unify(TConst.Bool, TVar(4))
  => Subst[3 -> int, 4 -> bool]
```

**Code Location**: `TypeInference.cs` - `Unify` method

**Needed**:
```csharp
private Subst Unify(Type a, Type b)
{
    // ... existing cases

    if (a is TAdt adt1 && b is TAdt adt2)
    {
        if (adt1.Name != adt2.Name)
            throw new Exception($"Cannot unify {adt1.Name} with {adt2.Name}");

        if (adt1.TypeArgs.Count != adt2.TypeArgs.Count)
            throw new Exception($"Type argument count mismatch: {adt1} vs {adt2}");

        var s = new Subst();
        for (int i = 0; i < adt1.TypeArgs.Count; i++)
        {
            var si = Unify(s.Apply(adt1.TypeArgs[i]), s.Apply(adt2.TypeArgs[i]));
            s.Compose(si);
        }
        return s;
    }

    // ...
}
```

**Impact**:
- Medium risk - extends existing unification
- Must handle occurs check in type arguments

---

### 9. Evaluator Changes (Evaluator.cs)

**Good News**: Runtime representation likely needs **no changes**!

`AdtVal` stores constructor name and payload:
```csharp
public sealed class AdtVal : Value
{
    public string Ctor;
    public Value? Payload;
    // ...
}
```

This is **type-erased** - no type information at runtime. Parametric types only matter during **compile-time type checking**.

```ocaml
(* Both create same runtime value: *)
SOME 42        (* AdtVal("SOME", IntVal(42)) *)
SOME "hello"   (* AdtVal("SOME", StringVal("hello")) *)
```

**Impact**: ✅ **Zero risk** - runtime unaffected

---

### 10. Pretty Printing

**Current**: `TAdt.ToString()` already handles type args:
```csharp
public override string ToString() => TypeArgs.Count==0 ? Name : Name + "<" + string.Join(", ", TypeArgs.Select(a=>a.ToString())) + ">";
```

**Needed**: Update to use Standard ML syntax `'a list` instead of `list<'a>`:
```csharp
public override string ToString()
{
    if (TypeArgs.Count == 0) return Name;

    // Single arg: 'a list
    if (TypeArgs.Count == 1)
        return TypeArgs[0].ToString() + " " + Name;

    // Multiple args: ('a, 'b) either
    return "(" + string.Join(", ", TypeArgs.Select(a => a.ToString())) + ") " + Name;
}
```

**Impact**: Low risk - cosmetic change

---

## Implementation Roadmap

### Phase 1: Foundation (Low Risk, 1-2 days)
1. ✅ Add `TypeParams` field to `TypeDecl` in Syntax.cs
2. ✅ Add `TypeVar` AST node in Syntax.cs
3. ✅ Update `Adts` dictionary to store type parameters
4. ✅ Add tests for data structure changes

### Phase 2: Parser (Medium Risk, 2-3 days)
1. 🔧 Update `ParseTypeDecl()` to parse `'a` and `('a, 'b)` syntax
2. 🔧 Update `ParseTypeExpr()` to handle `TypeVar` (`'a`)
3. 🔧 Add comprehensive parser tests:
   - `datatype 'a option = NONE | SOME of 'a`
   - `datatype ('a, 'b) either = Left of 'a | Right of 'b`
   - `datatype 'a tree = Leaf | Node of 'a * 'a tree * 'a tree`
4. 🔧 Test backward compatibility: `datatype color = Red | Green | Blue`

### Phase 3: Module System (Medium Risk, 2-3 days)
1. 🔧 Update `TypeFromTypeExpr` to accept type parameter context
2. 🔧 Update `LoadDecls` to create type variables for parameters
3. 🔧 Update constructor type generation to quantify over parameters
4. 🔧 Test constructor types: `SOME : forall 'a. 'a -> 'a option`

### Phase 4: Type Inference (High Risk, 3-5 days)
1. ⚠️ Update `Unify` to handle parametric ADTs
2. ⚠️ Test unification: `option<int>` ~ `option<'a>`
3. ⚠️ Update pattern matching inference to instantiate schemes
4. ⚠️ Test pattern matching: `case SOME x of ...`
5. ⚠️ Update constructor application inference
6. ⚠️ Test constructor usage: `SOME 42`, `Left "error"`

### Phase 5: Testing & Refinement (Medium Risk, 2-3 days)
1. 🧪 Create comprehensive test suite:
   - Option type (existing tests already exist - just remove Skip!)
   - Either type
   - Binary tree
   - Nested parametric types: `option<either<int, string>>`
   - Multiple constructors with type parameters
2. 🧪 Test type errors:
   - `SOME 42 : string option` (should fail)
   - Mixing incompatible types in patterns
3. 🧪 Stress test: Complex nested types
4. 🐛 Fix bugs discovered during testing

### Phase 6: Documentation (Low Risk, 1 day)
1. 📝 Update MEMORY.md to remove parametric type limitation
2. 📝 Update STANDARD_ML_COMPARISON.md (60% -> 75% coverage)
3. 📝 Add examples to documentation
4. 📝 Migration guide for existing code (OptionInt -> option)

---

## Total Effort Estimate

**Breakdown**:
- Phase 1: 1-2 days
- Phase 2: 2-3 days
- Phase 3: 2-3 days
- Phase 4: 3-5 days (most risky)
- Phase 5: 2-3 days
- Phase 6: 1 day

**Total**: **11-17 days** (2-3.5 weeks)

**Risk Factors**:
- Type inference changes are tricky - may discover edge cases
- Pattern matching with polymorphic constructors can be subtle
- Occurs check must work with nested parametric types
- Existing code may rely on monomorphic ADT behavior

**Success Criteria**:
- ✅ Can define `datatype 'a option = NONE | SOME of 'a`
- ✅ Constructors have polymorphic types: `SOME : forall 'a. 'a -> 'a option`
- ✅ Can use `SOME 42 : int option`, `SOME "hi" : string option`
- ✅ Pattern matching works: `case opt of NONE -> ... | SOME x -> ...`
- ✅ Type errors detected: `SOME 42 : string option` fails
- ✅ All existing tests still pass (backward compatibility)
- ✅ 26 skipped Option tests can be unskipped and pass

---

## Code Duplication Elimination

Once implemented, can replace:

```ocaml
(* BEFORE - Multiple monomorphic types *)
datatype OptionInt = NoneInt | SomeInt of int
datatype OptionFloat = NoneFloat | SomeFloat of float
datatype OptionString = NoneString | SomeString of string
datatype OptionBool = NoneBool | SomeBool of bool

structure OptionInt = { (* 50 lines *) }
structure OptionFloat = { (* 50 lines *) }
structure OptionString = { (* 50 lines *) }
structure OptionBool = { (* 50 lines *) }
```

With:

```ocaml
(* AFTER - Single polymorphic type *)
datatype 'a option = None | Some of 'a

structure Option = {
  fun isSome opt = case opt of None -> false | Some _ -> true,
  fun isNone opt = not (isSome opt),
  fun map f opt = case opt of None -> None | Some x -> Some (f x),
  fun bind f opt = case opt of None -> None | Some x -> f x,
  fun getOr opt default = case opt of None -> default | Some x -> x,
  (* ... 40 more lines, works for ALL types ... *)
}
```

**Savings**: ~200 lines reduced to ~50 lines (75% reduction)

---

## Alternative: Minimal Implementation

If full parametric types are too complex, a **minimal viable version**:

1. ✅ Only support **single** type parameter: `'a`
2. ✅ Skip multi-parameter types: `('a, 'b) either`
3. ✅ Only parse `'a type_name` syntax (no parens)
4. ✅ Test with `option`, `list`, `tree` only

**Reduced Effort**: ~6-10 days instead of 11-17 days

**Tradeoff**: Can't define `either`, `map`, `result` with multiple type params

---

## Recommendation

**Full Implementation**: Worth the effort (11-17 days)

**Why**:
1. Eliminates 200+ lines of duplicated code
2. Enables 26 currently-skipped tests to pass
3. Brings AttoML from 60% to ~75% Standard ML compatibility
4. Makes AttoML significantly more practical
5. Infrastructure (TAdt.TypeArgs) already exists - just need to use it!

**When**:
- If AttoML is for learning/experiments: **Optional** (current workaround is fine)
- If AttoML aims for ML compatibility: **High Priority**
- If AttoML wants to reduce code duplication: **Medium Priority**

**Next Step**:
Start with Phase 1 (foundation) - low risk, enables experimentation with other phases.
