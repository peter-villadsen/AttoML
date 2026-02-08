# Mathematical Equivalence Rules Analysis

## Current Implementation

### 1. **Implemented Rules** (LaTeXRewrite.atto)

#### **Commutative Rules** (2 rules)
```ocaml
a + b ↔ b + a
a * b ↔ b * a
```
**Purpose**: Allow reordering of operands to find best arrangement

#### **Associative Rules** (4 rules)
```ocaml
(a + b) + c ↔ a + (b + c)
(a * b) * c ↔ a * (b * c)
```
**Both directions**: Enables regrouping for factoring and simplification

#### **Identity Rules** (5 rules)
```ocaml
0 + a → a
a + 0 → a
1 * a → a
a * 1 → a
a^1 → a
```
**Purpose**: Eliminate unnecessary operations

#### **Distribution/Factoring** (3 rules)
```ocaml
a(b + c) ↔ ab + ac    (distribution)
(a + b)c ↔ ac + bc    (distribution)
ab + ac → a(b + c)    (factoring - reverse)
```
**Purpose**: Enable both expansion and factoring

#### **Fraction Rules** (2 rules)
```ocaml
a * b^-1 → a / b           (prefer division)
a/c + b/c → (a+b)/c        (combine like fractions)
```
**Purpose**: Produce clean fraction notation

#### **Negation Rules** (2 rules)
```ocaml
--a → a                    (double negative)
a + (-b) → a - b           (prefer subtraction)
```
**Purpose**: Eliminate negation in favor of subtraction

#### **Power Rules** (1 rule)
```ocaml
a^n * a^m → a^(n+m)
```
**Purpose**: Combine powers of same base

#### **Trigonometric Identity** (1 rule)
```ocaml
sin²(x) + cos²(x) → 1
```
**Purpose**: Apply Pythagorean identity

**Total: 20 rewrite rules**

---

## 2. **Constant Folding** ✅ IMPLEMENTED

Constant folding **IS** implemented in `SymCalc.simplify` (lines 237-336):

```ocaml
fun simplify e =
  match e with
  | Expr.Add (a, b) ->
      let a1 = simplify a in
      let b1 = simplify b in
      if isConstExpr a1 then
        if isConstExpr b1 then
          Expr.Const (constValue a1 + constValue b1)  (* FOLD! *)
        else ...
```

**What gets folded:**
- ✅ Arithmetic: `2 + 3 → 5`, `4 * 5 → 20`
- ✅ Powers: `2^3 → 8`
- ✅ Trig: `sin(0) → 0`, `cos(0) → 1`
- ✅ Log: `log(e) → 1`
- ✅ Identities: `x + 0 → x`, `x * 1 → x`
- ✅ Zeros: `x * 0 → 0`

**Example:**
```
Input:  (2 + 3) * x
Simplify: 5 * x
```

Constant folding happens **before** e-graph optimization, so the e-graph only sees `5 * x`.

---

## 3. **Missing Rules** ❌

### **Logarithm Rules** ❌ NOT IMPLEMENTED

You asked about these specifically - they're **missing**:

```ocaml
(* Product rule *)
log(a * b) ↔ log(a) + log(b)

(* Quotient rule *)
log(a / b) ↔ log(a) - log(b)

(* Power rule *)
log(a^n) ↔ n * log(a)

(* Change of base - not needed for LaTeX *)
```

**Why missing?**
- The initial implementation focused on **algebraic** simplification
- Logarithm rules are **transcendental** (different domain)
- For LaTeX, `log(xy)` is often **more readable** than `log(x) + log(y)`

**Should they be added?**
- **Context-dependent!**
  - For calculus (differentiation): YES - split logs help
  - For algebra: NO - combined form clearer
  - **Solution**: Make them optional rules or use cost function to prefer one direction

### **Exponential Rules** ❌ NOT IMPLEMENTED

```ocaml
e^(a+b) ↔ e^a * e^b
e^(a*b) ↔ (e^a)^b
```

### **Additional Power Rules** ❌ PARTIALLY IMPLEMENTED

```ocaml
(a*b)^n ↔ a^n * b^n         (* Power distributes over multiplication *)
(a/b)^n ↔ a^n / b^n         (* Power distributes over division *)
(a^m)^n ↔ a^(m*n)           (* Power of power *)
a^(-n) ↔ 1/(a^n)            (* Negative exponent *)
```

Currently only have: `a^n * a^m → a^(n+m)` ✅

### **Zero/One Propagation** ❌ PARTIALLY IMPLEMENTED

```ocaml
a * 0 → 0                   (* Currently in SymCalc.simplify ✅ *)
0 * a → 0
0 / a → 0
a / a → 1                   (* Watch out for a=0! *)
a - a → 0
```

### **Subtraction Rules** ❌ NOT IMPLEMENTED

```ocaml
a - b ↔ a + (-b)            (* Convert to addition *)
-(a + b) ↔ -a + -b          (* Distribute negation *)
-(a - b) ↔ b - a            (* Negate subtraction *)
```

---

## 4. **Constant Ordering** ❌ NOT IMPLEMENTED

You asked: "Is there a rule that prefers constants at the start (ab2 -> 2ab)?"

**Answer: No, but we SHOULD add it!**

### **Current Behavior**

With only commutativity `a*b ↔ b*a`, the e-graph explores:
- `a*b*2`
- `a*2*b`
- `2*a*b` ← **preferred for LaTeX**
- `2*b*a`
- `b*a*2`
- `b*2*a`

But the **cost function doesn't distinguish** between them! They all have the same cost:
```
Cost(Mul(Mul(a,b),2)) = Cost(Mul(Mul(2,a),b)) = 4+4+1+1+1 = 11
```

### **Solution: Add Constant-First Ordering**

**Option 1: Conditional Rewrite Rules**

```ocaml
(* Prefer constant on left *)
mkRule ("const_left_mul",
  EPattern.PMul (pvar "?x", EPattern.PConst ?c),
  Template.TMul (Template.TConst ?c, tvar "?x"))
```

**Problem**: Pattern matching can't check "is ?x not a constant"

**Option 2: Cost Function Enhancement**

Modify cost to penalize non-canonical forms:

```ocaml
fun latexCost node =
  case node of
    (* Penalize constant on right *)
    ENode.EMul (a, b) ->
      let baseCost = 4.0 in
      if isConst b then baseCost + 1.0  (* Penalty! *)
      else baseCost
  | ...
```

**Problem**: Cost function only sees individual nodes, not their children

**Option 3: Post-Processing in LaTeX Formatter**

```ocaml
(* In LaTeX.atto *)
fun canonicalizeMultiplication e =
  case e of
    Expr.Mul (a, Expr.Const c) -> Expr.Mul (Expr.Const c, a)
  | Expr.Mul (a, b) -> Expr.Mul (canonicalizeMultiplication a,
                                  canonicalizeMultiplication b)
  | ...
```

**Best approach**: Option 3 - handle in formatter as a final pass

---

## 5. **Recommended Additions**

### **Priority 1: Constant Ordering**

Add constant-first canonicalization in `LaTeX.atto`:

```ocaml
fun moveConstantsLeft e =
  match e with
  | Expr.Mul (a, Expr.Const c) when not (isConstExpr a) ->
      Expr.Mul (Expr.Const c, moveConstantsLeft a)
  | Expr.Add (a, Expr.Const c) when not (isConstExpr a) ->
      Expr.Add (Expr.Const c, moveConstantsLeft a)
  | ...
```

**Result**: `x*y*2 → 2*x*y` automatically

### **Priority 2: Logarithm Rules (Optional)**

Make them **conditional** based on context:

```ocaml
(* Only for calculus/differentiation contexts *)
fun logarithmRules () = [
  mkRule ("log_product",
    EPattern.PLog (EPattern.PMul (pvar "?a", pvar "?b")),
    Template.TAdd (Template.TLog (tvar "?a"), Template.TLog (tvar "?b"))),

  mkRule ("log_quotient",
    EPattern.PLog (EPattern.PDiv (pvar "?a", pvar "?b")),
    Template.TSub (Template.TLog (tvar "?a"), Template.TLog (tvar "?b"))),

  mkRule ("log_power",
    EPattern.PLog (EPattern.PPow (pvar "?a", pvar "?n")),
    Template.TMul (tvar "?n", Template.TLog (tvar "?a")))
]
```

**Usage**:
```ocaml
let rules = algebraicRules () @ logarithmRules () in
let eg = EGraph.saturate (eg1, rules, maxIters) in
...
```

### **Priority 3: Additional Power Rules**

```ocaml
(* Power distributes over multiplication *)
mkRule ("pow_distrib_mul",
  EPattern.PPow (EPattern.PMul (pvar "?a", pvar "?b"), pvar "?n"),
  Template.TMul (Template.TPow (tvar "?a", tvar "?n"),
                 Template.TPow (tvar "?b", tvar "?n"))),

(* Power of power *)
mkRule ("pow_pow",
  EPattern.PPow (EPattern.PPow (pvar "?a", pvar "?m"), pvar "?n"),
  Template.TPow (tvar "?a", Template.TMul (tvar "?m", tvar "?n"))),

(* Negative exponent *)
mkRule ("neg_exp",
  EPattern.PPow (pvar "?a", EPattern.PNeg (pvar "?n")),
  Template.TDiv (Template.TConst 1.0, Template.TPow (tvar "?a", tvar "?n")))
```

### **Priority 4: Subtraction Normalization**

```ocaml
(* Normalize subtraction to addition of negation *)
mkRule ("sub_to_add",
  EPattern.PSub (pvar "?a", pvar "?b"),
  Template.TAdd (tvar "?a", Template.TNeg (tvar "?b")))
```

**Caution**: This conflicts with `neg_to_sub` rule! The cost function will decide which wins.

---

## 6. **Testing the Rules**

Create test file `test_rules.atto`:

```ocaml
open LaTeX

(* Test constant folding *)
showOptimization "2 + 3"          (* Should be: 5 *)

(* Test factoring *)
showOptimization "a*b + a*c"      (* Should be: a(b+c) *)

(* Test fraction formation *)
showOptimization "x*y^-1"         (* Should be: \frac{x}{y} *)

(* Test fraction combination *)
showOptimization "x/z + y/z"      (* Should be: \frac{x+y}{z} *)

(* Test power laws *)
showOptimization "x^2 * x^3"      (* Should be: x^5 *)

(* Test trig identity *)
showOptimization "sin(x)^2 + cos(x)^2"  (* Should be: 1 *)

(* Test constant ordering - currently NOT handled *)
showOptimization "x*y*2"          (* Currently: x·y·2, Want: 2·x·y *)
```

---

## 7. **Architecture for Rule Categories**

Consider organizing rules by domain:

```ocaml
structure LaTeXRewrite = {
  fun algebraicRules () = [...],      (* Basic algebra *)
  fun logarithmRules () = [...],      (* Transcendental *)
  fun trigRules () = [...],           (* Trig identities *)
  fun powerRules () = [...],          (* Extended power laws *)

  (* Compose rule sets based on context *)
  fun standardRules () = algebraicRules () @ trigRules (),
  fun calculusRules () = standardRules () @ logarithmRules (),
  fun allRules () = algebraicRules () @ logarithmRules () @ trigRules () @ powerRules (),

  fun optimizeForLatex (expr, maxIters, ruleSet) =
    let eg0 = EGraph.empty 0 in
    let (rootId, eg1) = EGraph.add (eg0, expr) in
    let eg2 = EGraph.saturate (eg1, ruleSet (), maxIters) in
    EGraph.extract (eg2, latexCost, rootId)
}
```

---

## Summary Table

| Rule Category | Implemented | Missing | Priority |
|--------------|-------------|---------|----------|
| Commutativity | ✅ Add, Mul | - | - |
| Associativity | ✅ Add, Mul | - | - |
| Identity | ✅ 0, 1, ^1 | - | - |
| Distribution | ✅ Both ways | - | - |
| Factoring | ✅ Single var | Multi-var | Medium |
| Fractions | ✅ Basic | - | - |
| Negation | ✅ Double, to-sub | Distribute | Low |
| Powers | ✅ Add exponents | Distribute, nested | High |
| Logarithms | ❌ None | Product, quotient, power | Medium |
| Exponentials | ❌ None | Product, power | Low |
| Constant Order | ❌ None | Prefer left | **High** |
| Const Folding | ✅ SymCalc | - | - |
| Subtraction | ⚠️ Partial | Normalize forms | Medium |

## Recommendation

**Next steps**:
1. ✅ **Add constant-first ordering** (highest impact for LaTeX readability)
2. ✅ **Add extended power rules** (complete the power law coverage)
3. ⚠️ **Make logarithm rules optional** (context-dependent utility)
4. ⚠️ **Add subtraction normalization** (careful with cost function balance)
