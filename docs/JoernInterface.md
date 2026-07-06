# Plan: A Joern-style Interface for AttoML

Goal: build an app where a user submits Ocular/CPGQL-style queries against a
corpus of AttoML source files, backed by a Code Property Graph (CPG) built
from the AttoML frontend. This document is the design plan; no
implementation has started.

## Background: what Joern needs

Joern's core artifact is a CPG: AST + CFG + call graph + data-dependence
graph (DDG), plus overlays (per-node type info, containment), all as one
property graph, queried via CPGQL (the Scala/Gremlin-style fluent query
language; "Ocular" was the closed-source predecessor now folded into Joern).
"Add a Joern interface" means: build a CPG exporter for AttoML, then either
reuse Joern's real query engine or build a bespoke one.

## What AttoML already has

- Clean phase separation: lexer -> parser -> AST -> HM type inference ->
  module system -> evaluator (`src/AttoML.Core`, `src/AttoML.Interpreter`).
- Full static typing with Hindley-Milner inference already computed per
  program.
- A module concept (`structure` / `signature`, `Modules/ModuleSystem.cs`)
  that is a reasonable stand-in for a CPG namespace/project unit (see below).
- Pure, expression-oriented semantics: no mutation, no loops/goto,
  no assignment. This removes most of the complexity of imperative CFG/DDG
  construction, but means "CFG" must be defined from scratch (see Gap 4).

## Gaps and required changes to existing code

1. **No source positions on the AST.** `Token` has a raw char `Position`
   (`src/AttoML.Core/Lexer/Token.cs`), but `Expr` / `Pattern` / `ModuleDecl`
   (`src/AttoML.Core/Parsing/Syntax.cs`) carry nothing; the parser discards
   it. Every CPG node needs file/line/column.
   - Fix: add a `SourceSpan? Span` field to the `Expr`, `Pattern`, and
     `ModuleDecl` base classes; set it at each construction site in
     `Parser.cs` (~1230 lines, many sites — the most invasive existing-code
     change in this plan).

2. **No per-subexpression type map.** `TypeInference.InferExpr`
   (`src/AttoML.Core/Types/TypeInference.cs`) recurses and returns a type per
   call but never records it against the node.
   - Fix: add a `Dictionary<Expr, Type>` populated during inference, needed
     for CPG `TYPE_FULL_NAME` per node.

3. **No stable node identity.** CPG nodes need IDs for edges.
   - Fix: a post-parse pass that walks the tree once and assigns a `long` ID
     into a side table (avoid baking IDs into every AST class).

4. **No CFG concept.** AttoML has no statements/loops/assignment, so "control
   flow" must be defined as evaluation order over expressions rather than the
   classic imperative basic-block graph:
   - sequence edges before each subexpression evaluation,
   - branch edges after `IfThenElse` / `Match` conditions with a join after,
   - exceptional edges for `Raise` / `Handle` (similar to Joern's try/catch
     flow in other frontends).
   - This is the largest new subsystem to build; there is no drop-in reuse
     from the existing evaluator.

5. **Module system needs a multi-file driver, not new resolution logic.**
   `ModuleSystem.LoadDecls` accumulates into instance dictionaries
   (`Structures`, `Signatures`, `Adts`, `Exceptions`) and does not reset
   between calls, so feeding decls from multiple files into one
   `ModuleSystem` before calling `InjectStructuresInto` already resolves
   cross-file `open` correctly. What's missing is thinner than a new project
   system:
   - `Frontend.Compile(string source)` builds a fresh `ModuleSystem`
     per call and only takes one source string — add a
     `Frontend.CompileCorpus(IEnumerable<(string file, string source)>)`
     entry point that parses each file and loads them into one shared
     `ModuleSystem`.
   - `StructureDecl` / `StructureInfo` / `Binding` don't record which file
     they came from — add file provenance (same pass as source spans).
   - `Structures[st.Name] = ...` silently overwrites on name collision
     across files — add duplicate-name detection when loading a corpus.
   - Structures don't nest (flat namespace only) — for a corpus, map each
     file to exactly one namespace rather than attempting nested modules.
   - Maps onto the CPG as: `structure` -> `NAMESPACE_BLOCK`, file ->
     `FILE` node (containment edge), bindings -> `METHOD` (for `Fun` /
     `LetRec`) or `MEMBER` (plain values), `signature` -> interface/type-decl
     node, `open` -> import edge. Top-level bindings outside any `structure`
     get a synthesized default namespace.

## Properties that are easier here than in Joern's usual targets

- **Call graph**: most calls are syntactically direct (`Var` / `Qualify`
  referring to a top-level binding), so the call graph is largely
  static-resolvable without the dynamic-dispatch machinery Joern needs for
  JS/Python/Java. Higher-order calls (e.g. `List.map f xs`) are the only
  place needing approximate/points-to handling.
- **Data-dependence graph**: bindings are immutable (`let`, params, pattern
  vars), so "reaching definition" is just the unique lexical binding site —
  DDG edges are def-site -> each `Var` use in scope, computable directly
  from the (now-typed, spanned) AST without abstract interpretation.

## Query-engine decision (needs a choice before implementation starts)

Two paths to support real Ocular/CPGQL query syntax:

- **(A) Export to Joern's native CPG format** and let real Joern (JVM)
  execute queries against it. Only the exporter needs to be written; the
  full query language and standard library come for free. Cost: a JVM
  dependency in the deployment; the app becomes a pipeline (parse -> build
  CPG -> export -> shell to / embed Joern server -> forward query text ->
  render results).
- **(B) Reimplement a query layer in C#** (in-memory graph + LINQ-based
  fluent API mimicking `cpg.method.name(...)`, or a graph DB with
  openCypher). Fully self-contained, no JVM, but not real CPGQL syntax and
  reinvents a nontrivial DSL.

Recommendation: (A), since "submit Ocular queries" implies wanting the real
syntax rather than a lookalike, and it is significantly less code.

## Build order

1. Source spans on AST + node-ID assignment pass.
2. Per-node type table from `TypeInference`.
3. Multi-file corpus driver on top of the existing `ModuleSystem`
   (file provenance + collision detection), per Gap 5.
4. AST -> CPG node/edge emitter (FILE, NAMESPACE_BLOCK, METHOD, PARAM, CALL,
   LITERAL, IDENTIFIER, CONTROL_STRUCTURE for `if`/`match`, TYPE_DECL for
   ADTs).
5. CFG builder over the same tree (evaluation-order fragments with holes,
   branch/join for `if`/`match`, exceptional edges for `raise`/`handle`).
6. Call graph + DDG edges (leveraging the simplifications above).
7. Export to Joern's CPG format (path A) or build query layer (path B) —
   pending the decision above.
8. The app itself: ingest a directory of `.atto` files -> build CPG -> serve
   a query box + results view.

## Open decisions before implementation

- Confirm path (A) vs (B) for the query engine.
- Confirm one-namespace-per-file convention for the corpus driver (Gap 5).
- Confirm synthesized-name convention for anonymous lambdas and
  top-level-outside-any-structure bindings when mapped to CPG METHOD nodes.
