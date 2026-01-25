# Architecture Overview

Projects:
- `AttoML.Core`: Frontend (lexing, parsing, AST, types, HM inference), modules, `Frontend` driver.
- `AttoML.Interpreter`: Runtime values, environments, evaluator, built-ins, REPL.
- `AttoML.Tests`: xUnit tests for frontend, typing, evaluation, and modules.

Data Structures:
- Tokens: `TokenKind`, `Token`
- AST: `Expr` variants (literals, `Var`, `Fun`, `App`, `Let`, `LetRec`, `IfThenElse`, `Tuple`, `List`, `Record`, `Qualify`, `Match`), module decls (`StructureDecl`, `SignatureDecl`, `OpenDecl`), `Pattern` nodes (wildcard, var, literals, tuple, constructor)
- Types: `Type` variants (`TVar`, `TConst`, `TFun`, `TTuple`, `TList`, `TRecord`, `TAdt`), `Scheme`, `Subst`, `TypeEnv`
- Values: runtime `Value` variants and `Env`

Separation of Phases:
- Lexing: `Lexer`
- Parsing: `Parser`
- Typing: `TypeInference`
- Evaluation: `Evaluator`
- Module handling: `ModuleSystem`

Reuse: The frontend is designed for reuse by a future compiler backend.
