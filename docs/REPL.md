# REPL Usage

- Prompt shows `>>` for the first line and `..` for subsequent lines in a multi-line block.
- Enter one or more lines, then either:
  - Press Enter on an empty line, or
  - Type `;;` on a line by itself

This submits the accumulated block for compilation and execution.

The REPL also auto-continues only when delimiters are unbalanced (e.g., an open `(`, `{`, or `[` without a matching close) or when a line ends with a trailing `\`. Otherwise, single lines are executed immediately without requiring a blank line.

Top-level convenience:
- You can define named functions with sugar:
  - `fun add x y = x + y` defines a curried function `add`.
  - `fun addPair (x, y) = x + y` defines a function that pattern-matches a tuple parameter.
  - These desugar to `val` bindings with nested `fun`s (and `match` for tuple patterns).

Examples:

```
>> val x = 1
.. val y = 2
.. ;;
val x : int = 1
val y : int = 2
val it : int = 2
>> x + y
val it : int = 3
>> (1 / 0) handle Div -> 42
val it : int = 42
>> (Math.sqrt (0.0 - 1.0)) handle Domain -> 0.0
val it : float = 0
```
