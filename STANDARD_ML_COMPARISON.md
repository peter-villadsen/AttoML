# AttoML vs Standard ML: Feature Comparison

## Executive Summary

AttoML implements a **substantial subset** of Standard ML, with strong coverage of core functional programming features:
- ✅ **Complete**: Type inference, ADTs, pattern matching, higher-order functions, basic modules
- ⚠️ **Partial**: Module system (no functors), equality types (no constraints)
- ❌ **Missing**: Mutable references, I/O, separate compilation, parametric modules

**Overall Coverage**: ~60-70% of Standard ML's features, focused on pure functional programming.

---

## 1. TYPE SYSTEM FEATURES

### ✅ **Fully Implemented**

| Feature | AttoML | Standard ML | Notes |
|---------|--------|-------------|-------|
| **Type Inference** | ✅ | ✅ | Full Hindley-Milner with occurs check |
| **Type Variables** | ✅ | ✅ | `'a`, `'b` work correctly |
| **Function Types** | ✅ | ✅ | `int -> int -> bool` |
| **Tuple Types** | ✅ | ✅ | `int * string * bool` |
| **List Types** | ✅ | ✅ | `int list`, `'a list` |
| **Record Types** | ✅ | ✅ | `{x: int, y: bool}` |
| **ADT Types** | ✅ | ✅ | `datatype t = C1 \| C2 of int` |
| **Type Schemes** | ✅ | ✅ | Rank-0 polymorphism |
| **Type Annotations** | ✅ | ✅ | `val x : int = 5` |

### ⚠️ **Partially Implemented**

| Feature | AttoML | Standard ML | Gap |
|---------|--------|-------------|-----|
| **Equality Types** | ❌ | ✅ | No `''a` constraint for equality types |
| **Type Abbreviations** | ❌ | ✅ | No `type name = existing_type` |
| **Transparent vs Opaque** | ❌ | ✅ | No `:` vs `:>` distinction in signatures |

### ❌ **Not Implemented**

| Feature | Standard ML | Why Missing |
|---------|-------------|-------------|
| **Parametric Modules** | `datatype 'a option = ...` | See note below on parametric types |
| **Type Sharing** | `sharing type t1 = t2` | No functor system |
| **Abstraction Types** | `abstype t = ... with ... end` | Limited encapsulation |
| **Type Constraints in Signatures** | `eqtype t` | No equality type tracking |
| **Overloading Resolution** | Numeric literals | Runtime type checking instead |

**Note on Parametric Types**: AttoML's ADTs are **monomorphic**. While built-in types like `list` are polymorphic (`'a list`), user-defined datatypes cannot be parameterized. This means:

```ocaml
(* Standard ML - WORKS *)
datatype 'a option = NONE | SOME of 'a

(* AttoML - DOES NOT WORK *)
datatype OptionInt = NoneInt | SomeInt of int  (* Must define per type *)
datatype OptionFloat = NoneFloat | SomeFloat of float
```

This is a **fundamental limitation** affecting reusability and generality.

---

## 2. EXPRESSION SYNTAX

### ✅ **Fully Implemented**

```ocaml
(* All of these work in AttoML *)
let x = 42 in x + 1                    (* Let bindings *)
let rec factorial n = ...              (* Recursion *)
fun x -> x + 1                         (* Lambdas *)
fn x => x + 1                          (* Alternative syntax *)
if x > 0 then "pos" else "neg"        (* Conditionals *)
match x with 0 -> "zero" | _ -> "nonzero"  (* Pattern matching *)
case x of 0 => "zero" | _ => "nonzero"     (* Alternative syntax *)
(1, "two", true)                       (* Tuples *)
[1, 2, 3]                              (* Lists *)
{x = 5, y = true}                      (* Records *)
rec.x                                  (* Record field access *)
raise (Fail "error")                   (* Exception raising *)
try expr handle Fail msg -> 0          (* Exception handling *)
```

### ⚠️ **Partially Implemented**

| Feature | AttoML | Standard ML | Gap |
|---------|--------|-------------|-----|
| **Let-Patterns** | `let (x, y) = e in ...` | ✅ | Works, but limited in structure functions |
| **Function Patterns** | `fun (x, y) -> x + y` | ✅ | Desugars to pattern match |
| **Multiple Clauses** | ❌ | `fun f 0 = 1 \| f n = ...` | Must use explicit match |
| **Or-Patterns** | ❌ | `x \| y` | Must duplicate branches |
| **As-Patterns** | ❌ | `x as (h::t)` | Must bind twice |
| **Layered Patterns** | ❌ | `x as pat` | Must bind twice |
| **Guards** | ❌ | `pat if cond -> expr` | Must nest if-then-else |

### ❌ **Not Implemented**

| Feature | Standard ML Example | Why Missing |
|---------|---------------------|-------------|
| **Local Declarations** | `let val x = 1 val y = 2 in ... end` | Parser limitation |
| **Where Clauses** | `expr where fun f x = ...` | Not parsed |
| **Sequence Expressions** | `(expr1; expr2; expr3)` | No imperative features |
| **While Loops** | `while cond do expr` | No mutation |
| **Case Exhaustiveness Warning** | Warns on non-exhaustive match | Runtime error instead |

**Example - Multiple Function Clauses**:
```ocaml
(* Standard ML *)
fun length [] = 0
  | length (h::t) = 1 + length t

(* AttoML - Must write *)
fun length lst = match lst with
    [] -> 0
  | h::t -> 1 + length t
end
```

---

## 3. MODULE SYSTEM

### ✅ **Fully Implemented**

```ocaml
(* Basic structures work *)
structure M = {
  val x = 10
  fun f y = y + 1
}

(* Signatures work *)
signature S = {
  val x : int
  val f : int -> int
}

(* Opening modules works *)
open M
val z = x + 1  (* Can access M.x without qualification *)
```

### ❌ **Major Missing Features**

| Feature | Standard ML | AttoML | Impact |
|---------|-------------|--------|--------|
| **Functors** | ✅ | ❌ | **Critical** - Cannot parameterize modules |
| **Functor Application** | `structure M = F(A)` | ❌ | No code reuse mechanism |
| **Where Type Clauses** | `sig ... where type t = int` | ❌ | Limited refinement |
| **Signature Ascription** | `:` vs `:>` (transparent/opaque) | ❌ | No encapsulation control |
| **Include** | `include S` in structures | ❌ | No signature composition |
| **Datatype Replication** | `datatype t = datatype M.t` | ❌ | Cannot re-export types |

**Example - Functors Missing**:
```ocaml
(* Standard ML - Parameterized data structures *)
functor SetFn(Ord: ORDERED) = struct
  type elem = Ord.t
  type set = elem list
  fun insert x s = ...
  fun member x s = ...
end

structure IntSet = SetFn(IntOrder)
structure StringSet = SetFn(StringOrder)

(* AttoML - Must duplicate code for each type *)
structure IntSet = {
  type elem = int
  fun insert x s = ...  (* Duplicate implementation *)
}

structure StringSet = {
  type elem = string
  fun insert x s = ...  (* Duplicate implementation *)
}
```

**Impact**: This is a **major limitation** for building reusable abstractions. Without functors, you must:
- Duplicate code for each type variant
- Cannot abstract over implementations (e.g., different tree structures)
- No dependency injection mechanism

---

## 4. PATTERN MATCHING

### ✅ **Implemented Patterns**

| Pattern Type | Example | Works? |
|--------------|---------|--------|
| **Wildcard** | `_` | ✅ |
| **Variable** | `x` | ✅ |
| **Literal** | `5`, `true`, `"hi"` | ✅ |
| **Tuple** | `(x, y, z)` | ✅ |
| **List** | `[1, 2, x]` | ✅ |
| **Cons** | `h::t`, `x::y::rest` | ✅ |
| **Record** | `{x, y}`, `{x=a, y=b}` | ✅ |
| **Constructor** | `Some x`, `None` | ✅ |
| **Qualified Constructor** | `Option.Some x` | ✅ |

### ❌ **Missing Pattern Features**

| Feature | Standard ML | AttoML | Workaround |
|---------|-------------|--------|------------|
| **Or-Patterns** | `Red \| Blue \| Green` | ❌ | Duplicate branches |
| **As-Patterns** | `x as (h::t)` | ❌ | Bind in body |
| **Guards** | `pat if x > 0` | ❌ | Nest if-then-else |
| **Nested Or** | `(1\|2, 3\|4)` | ❌ | Enumerate all combinations |
| **Lazy Patterns** | `lazy pat` | ❌ | N/A - no laziness |

**Example - Or-Patterns Missing**:
```ocaml
(* Standard ML *)
fun isWeekend Saturday | Sunday = true
  | isWeekend _ = false

(* AttoML - Must write *)
fun isWeekend day = match day with
    Saturday -> true
  | Sunday -> true
  | _ -> false
end
```

---

## 5. IMPERATIVE FEATURES

### ❌ **Completely Missing**

AttoML is **purely functional** with **no mutation** whatsoever.

| Feature | Standard ML | AttoML | Impact |
|---------|-------------|--------|--------|
| **References** | `ref`, `:=`, `!` | ❌ | **Critical** - No mutable state |
| **Arrays** | `Array.array`, `Array.sub`, `Array.update` | ❌ | Must use persistent structures |
| **Sequences** | `expr1; expr2; expr3` | ❌ | Cannot sequence side effects |
| **While Loops** | `while cond do expr` | ❌ | Must use recursion |
| **Assignment** | `r := value` | ❌ | N/A |
| **Input/Output** | `print`, `TextIO.input`, etc. | ❌ | **Critical** - No I/O |

**Example - References Missing**:
```ocaml
(* Standard ML - Mutable counter *)
val counter = ref 0
fun increment() = (counter := !counter + 1; !counter)

(* AttoML - Must use functional approach *)
fun incrementState count = count + 1
(* Caller must thread state through program *)
```

**Example - I/O Missing**:
```ocaml
(* Standard ML *)
val _ = print "Hello, world!\n"
val line = TextIO.inputLine TextIO.stdIn

(* AttoML - NO I/O POSSIBLE *)
(* Can only work with pure data transformations *)
```

**Impact**: This is **extremely limiting** for practical programs:
- ❌ Cannot read from files or stdin
- ❌ Cannot write to files or stdout (except implicit REPL output)
- ❌ Cannot implement stateful algorithms without threading state
- ❌ Cannot use arrays for efficient imperative algorithms
- ❌ Cannot interface with external systems

---

## 6. BUILTIN MODULES AND STANDARD BASIS

### ✅ **Implemented (AttoML Custom)**

| Module | Functions | Coverage |
|--------|-----------|----------|
| **Base** | `add`, `sub`, `mul`, `div`, `eq`, `lt`, `and`, `or`, `not` | ~30% of Basis |
| **List** | `map`, `filter`, `foldl`, `foldr`, `head`, `tail`, `append`, etc. | ~60% of Basis List |
| **String** | `concat`, `size`, `substring`, `explode`, `implode`, etc. | ~40% of Basis String |
| **Math** | `sin`, `cos`, `sqrt`, `log`, `exp`, `pi`, etc. | ~50% of Basis Math |
| **Tuple** | `fst`, `snd`, `swap`, `curry`, `uncurry` | Custom (not in SML Basis) |
| **Set** | Integer sets only | Custom (limited) |
| **Map** | Integer-to-integer maps only | Custom (limited) |
| **Complex** | Complex number operations | Custom (not in SML Basis) |

### ❌ **Missing Standard Basis Libraries**

| Library | Standard ML | AttoML | Impact |
|---------|-------------|--------|--------|
| **TextIO** | File and console I/O | ❌ | **Critical** - No I/O |
| **BinIO** | Binary I/O | ❌ | N/A |
| **OS** | Operating system interface | ❌ | **Critical** - No file system |
| **Array** | Mutable arrays | ❌ | Must use persistent structures |
| **Array2** | 2D arrays | ❌ | N/A |
| **Vector** | Immutable vectors | ❌ | Lists instead |
| **Word** | Bitwise operations | ❌ | No low-level programming |
| **Real** | Floating-point control | ❌ | Basic floats only |
| **Int** | Integer operations | Partial | Basic operations only |
| **Char** | Character operations | ❌ | Strings instead |
| **Substring** | Efficient string slicing | ❌ | Must copy strings |
| **Timer** | Timing operations | ❌ | No profiling |
| **Date** | Date/time operations | ❌ | N/A |
| **Time** | Time span operations | ❌ | N/A |
| **CommandLine** | Command-line arguments | ❌ | N/A |
| **Socket** | Network I/O | ❌ | N/A |

**Impact**: Without the Standard Basis, AttoML is suitable only for:
- ✅ Pure algorithmic problems
- ✅ Mathematical computations
- ✅ Data structure implementations (persistent)
- ✅ Type-level programming demonstrations
- ❌ **NOT suitable** for systems programming, scripting, or practical applications

---

## 7. ADVANCED FEATURES

### ❌ **Not Implemented**

| Feature | Standard ML | Use Case | Workaround |
|---------|-------------|----------|------------|
| **Lazy Evaluation** | `lazy expr` / `force` | Infinite lists, memoization | Strict evaluation only |
| **Type Classes** | N/A in SML either | Ad-hoc polymorphism | Runtime type checking |
| **Separate Compilation** | CM files, MLB files | Large projects | Monolithic compilation |
| **Interactive Loop Directives** | `use "file.sml"` | Load files | Manual concatenation |
| **Overloading** | Numeric literals resolve by context | Convenience | Explicit type annotations |
| **Infix Declarations** | `infix 6 @@` | Custom operators | Prefix calls only |
| **Value Polymorphism Restriction** | Prevents generalization of refs | Type safety | No refs, so N/A |
| **Type Annotation on Patterns** | `(x: int)` in patterns | Constrain types | Annotate binding instead |

---

## 8. SYNTAX DIFFERENCES

### Minor Syntax Variations

| Feature | Standard ML | AttoML | Compatible? |
|---------|-------------|--------|-------------|
| **Arrow Syntax** | `=>` | `->` or `=>` | Both supported ✅ |
| **Match Terminator** | Implicit | `end` optional | Compatible ✅ |
| **Comment Style** | `(* ... *)` | `(* ... *)` | Same ✅ |
| **List Cons** | `::` | `::` | Same ✅ |
| **List Append** | `@` | `@` | Same ✅ |
| **String Concat** | `^` | `^` | Same ✅ |
| **Tuple Constructor** | `(a, b)` | `(a, b)` | Same ✅ |
| **Record Syntax** | `{x=1, y=2}` | `{x=1, y=2}` | Same ✅ |
| **Unit Value** | `()` | `()` | Same ✅ |

### Incompatible Syntax

| Feature | Standard ML | AttoML | Impact |
|---------|-------------|--------|--------|
| **Multi-Clause Functions** | `fun f pat1 = e1 \| f pat2 = e2` | Must use match | Minor inconvenience |
| **Local Declarations** | `let val x = 1 val y = 2 in ...` | Must nest lets | Verbose |
| **Sequential Let** | `let val x = 1 val y = x in ...` | Must nest lets | Verbose |

**Example - Multi-Clause Functions**:
```ocaml
(* Standard ML - Concise *)
fun fib 0 = 1
  | fib 1 = 1
  | fib n = fib (n-1) + fib (n-2)

(* AttoML - More verbose *)
fun fib n = match n with
    0 -> 1
  | 1 -> 1
  | n -> fib (n-1) + fib (n-2)
end
```

---

## 9. CRITICAL LIMITATIONS SUMMARY

### Top 5 Most Impactful Missing Features

1. **No I/O (TextIO, BinIO)**
   - **Impact**: 🔴 **CRITICAL** - Cannot build practical programs
   - **Use Cases Blocked**: File processing, user interaction, logging, debugging
   - **Workaround**: None - fundamental limitation

2. **No Mutable References (`ref`)**
   - **Impact**: 🔴 **CRITICAL** - Cannot implement stateful algorithms efficiently
   - **Use Cases Blocked**: Imperative algorithms, caching, memoization with state
   - **Workaround**: Thread state through function calls (verbose, inefficient)

3. **No Parametric ADTs (`datatype 'a t`)**
   - **Impact**: 🟡 **HIGH** - Must duplicate code for each type variant
   - **Use Cases Blocked**: Generic data structures (Option, Result, Tree, etc.)
   - **Workaround**: Monomorphic types per concrete type (OptionInt, OptionFloat, etc.)

4. **No Functors (Parameterized Modules)**
   - **Impact**: 🟡 **HIGH** - Cannot abstract over implementations
   - **Use Cases Blocked**: Generic algorithms, dependency injection, code reuse
   - **Workaround**: Duplicate implementations (significant code bloat)

5. **No Arrays**
   - **Impact**: 🟡 **MEDIUM** - Must use persistent structures even when inefficient
   - **Use Cases Blocked**: Imperative algorithms, matrix operations, graph algorithms
   - **Workaround**: Use lists (O(n) access instead of O(1))

### Features That Are Less Critical But Notable

- **No Separate Compilation**: All code must be in one file or manually concatenated
- **No Overloading Resolution**: Numeric literals require type annotations in ambiguous contexts
- **No Type Classes/Equality Types**: Cannot constrain polymorphic functions to equality types
- **No Guard Patterns**: Must nest if-then-else inside match branches
- **No Lazy Evaluation**: Cannot define infinite data structures
- **No OS Interface**: Cannot access file system, environment variables, or external processes

---

## 10. WHAT ATTOML IS GOOD FOR

Despite the limitations, AttoML excels at:

### ✅ **Excellent Use Cases**

1. **Learning Functional Programming**
   - Clean, understandable type inference
   - Good error messages for type mismatches
   - Interactive REPL for experimentation
   - Pattern matching demonstrations

2. **Pure Algorithms and Data Structures**
   - Persistent data structures (lists, trees, maps)
   - Recursive algorithms
   - Higher-order functions (map, filter, fold)
   - Algorithm prototyping

3. **Mathematical Computations**
   - Expression evaluation and simplification (demonstrated by SymCalc)
   - Symbolic computation (demonstrated by E-Graph implementation)
   - Numerical algorithms (Math module)
   - Complex number operations

4. **Type System Demonstrations**
   - Hindley-Milner type inference
   - Polymorphic functions
   - Algebraic data types
   - Pattern matching exhaustiveness

5. **Compiler/Interpreter Building Blocks**
   - AST representations
   - Pattern matching on ASTs
   - Type checking algorithms
   - Evaluation strategies

### ❌ **Poor Use Cases**

1. **Systems Programming** - No I/O, no OS interface
2. **Scripting** - No file reading/writing, no command-line arguments
3. **Web Development** - No network I/O, no HTTP libraries
4. **Data Processing** - No file I/O, limited string operations
5. **GUI Applications** - No event loops, no mutable state
6. **Performance-Critical Code** - No arrays, no mutable data structures
7. **Large Projects** - No separate compilation, no build system

---

## 11. ROADMAP TO FULL STANDARD ML COMPATIBILITY

If AttoML were to evolve toward full Standard ML compatibility, here's a suggested roadmap:

### Phase 1: Core Language Completeness (High Value, Medium Effort)
1. ✅ **Already done** - Core ML syntax, type inference, ADTs
2. **Add Multi-Clause Function Syntax** - `fun f pat1 = e1 | f pat2 = e2`
3. **Add Guard Patterns** - `pat if cond -> expr`
4. **Add Or-Patterns** - `Red | Blue | Green`
5. **Add As-Patterns** - `x as pat`
6. **Improve Error Messages** - Better type mismatch explanations

### Phase 2: Module System (High Value, High Effort)
1. **Implement Functors** - Parameterized modules
2. **Add Signature Refinement** - `where type t = ...`
3. **Transparent vs Opaque Ascription** - `:` vs `:>`
4. **Add Include** - Signature composition
5. **Datatype Replication** - `datatype t = datatype M.t`

### Phase 3: Imperative Features (Critical for Practical Use, Medium Effort)
1. **Add References** - `ref`, `:=`, `!` operators
2. **Add Sequence Expressions** - `expr1; expr2; expr3`
3. **Add While Loops** - `while cond do expr`
4. **Add Arrays** - Mutable array type with operations

### Phase 4: I/O and Standard Basis (Critical for Practical Use, High Effort)
1. **Add TextIO Module** - File and console I/O
2. **Add CommandLine Module** - Command-line arguments
3. **Add OS Module** - File system operations
4. **Expand String Module** - Full Basis compatibility
5. **Add Vector Module** - Immutable vectors
6. **Add Char Module** - Character operations

### Phase 5: Type System Enhancements (Medium Value, Very High Effort)
1. **Parametric ADTs** - `datatype 'a option = NONE | SOME of 'a`
2. **Equality Types** - `''a` constraint tracking
3. **Type Abbreviations** - `type name = existing_type`
4. **Overloading Resolution** - Numeric literal specialization
5. **Abstraction Types** - `abstype` declarations

### Phase 6: Advanced Features (Low Value, High Effort)
1. **Lazy Evaluation** - `lazy` / `force` primitives
2. **Separate Compilation** - Module file system
3. **Infix Declarations** - Custom operator precedence
4. **Type Classes** - Ad-hoc polymorphism (not in SML, but valuable)

**Estimated Effort**:
- Phase 1: ~2-4 weeks
- Phase 2: ~4-8 weeks
- Phase 3: ~2-4 weeks
- Phase 4: ~8-12 weeks
- Phase 5: ~12-20 weeks (requires significant type system rewrite)
- Phase 6: ~4-8 weeks

**Total to Full Compatibility**: ~32-56 weeks (8-14 months of development)

---

## 12. CONCLUSION

### Overall Assessment

**AttoML Coverage**: ~60-70% of Standard ML

| Category | Coverage | Assessment |
|----------|----------|------------|
| **Core Language** | 90% | Excellent - nearly complete |
| **Type System** | 70% | Good - missing parametric types |
| **Module System** | 40% | Limited - no functors |
| **Standard Basis** | 20% | Poor - missing I/O, arrays |
| **Imperative Features** | 0% | None - purely functional |
| **Advanced Features** | 10% | Minimal - missing many features |

### Strengths
- ✅ Clean, well-implemented type inference
- ✅ Good pattern matching support
- ✅ Solid functional programming core
- ✅ Useful for learning and prototyping
- ✅ Excellent for pure algorithmic problems
- ✅ Demonstrates advanced concepts (E-Graphs, symbolic computation)

### Critical Gaps
- ❌ No I/O (cannot read/write files)
- ❌ No mutation (no refs or arrays)
- ❌ No parametric ADTs (code duplication required)
- ❌ No functors (limited abstraction)
- ❌ No separate compilation (monolithic programs)

### Recommendation

**AttoML is an excellent:**
- Educational tool for functional programming
- Platform for algorithm prototyping
- Framework for compiler/interpreter experiments
- Demonstration of type inference and pattern matching

**AttoML is NOT suitable for:**
- Production applications
- Systems programming
- File processing or scripting
- Performance-critical code
- Large-scale software development

**To make AttoML practical**, prioritize:
1. **I/O (Phase 4)** - Absolutely critical for real programs
2. **Parametric ADTs (Phase 5)** - Eliminates code duplication
3. **References (Phase 3)** - Enables stateful algorithms
4. **Functors (Phase 2)** - Enables abstraction and reuse

AttoML represents a well-executed subset of Standard ML that captures the essence of functional programming while remaining tractable for a single-developer implementation. Its limitations are well-understood and primarily stem from design choices (pure functional, no I/O) rather than implementation quality.
