using System;
using System.IO;
using AttoML.Core;
using AttoML.Core.Parsing;
using AttoML.Interpreter.Builtins;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter
{
	internal class Program
	{
		static void Main(string[] args)
		{
			bool verbose = false;
			string? path = null;
			for (int i = 0; i < args.Length; i++)
			{
				var a = args[i];
				if (a == "-v" || a == "--verbose") { verbose = true; continue; }
				if (!a.StartsWith("-")) { path = a; break; }
			}
			if (path == null)
			{
				RunRepl(verbose);
			}
			else
			{
				var source = File.ReadAllText(path);
				EvaluateSource(source, verbose);
			}
		}

		static void EvaluateSource(string source, bool verbose)
		{
			var frontend = new Frontend();
			// Load prelude (Complex) before user source so it's available to consume
			LoadPrelude(frontend);
			var (decls, modules, expr, exprType) = frontend.Compile(source);
			var evaluator = new Evaluator();
			// Load builtins
			LoadBuiltins(evaluator);
			if (verbose)
			{
				foreach (var d in decls)
				{
					var dstr = AstPrinter.Print(d);
					Console.WriteLine($"AST decl: {dstr}");
				}
			}
			evaluator.LoadModules(modules);
			evaluator.LoadAdts(modules);
			evaluator.LoadExceptions(modules);
			evaluator.ApplyOpen(decls);
			// Handle top-level val declarations
			var valTypes = frontend.InferTopVals(modules, decls);
			Value? lastVal = evaluator.ApplyValDecls(decls);
			for (int i = 0; i < valTypes.Count; i++)
			{
				var (name, ty) = valTypes[i];
				evaluator.GlobalEnv.TryGet(name, out var vv);
				Console.WriteLine($"val {name} : {ty} = {vv}");
			}
			if (expr != null)
			{
				if (verbose)
				{
					var ast = AttoML.Core.Parsing.AstPrinter.Print(expr);
					Console.WriteLine($"AST: {ast}");
				}
				var v = evaluator.Eval(expr, evaluator.GlobalEnv);
				evaluator.GlobalEnv.Set("it", v);
				var typeStr = exprType?.ToString() ?? "<unknown>";
				Console.WriteLine($"val it : {typeStr} = {v}");
				if (exprType != null)
				{
					// Seed 'it' type for subsequent lines
					frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), exprType));
				}
			}
			else if (lastVal != null)
			{
				// If only val declarations were present, ensure `it` stays printed with last value type (if available)
				if (valTypes.Count > 0)
				{
					var (name, ty) = valTypes[^1];
					Console.WriteLine($"val it : {ty} = {lastVal}");
					frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), ty));
				}
			}
		}

		static void RunRepl(bool verbose)
		{
			Console.WriteLine("AttoML REPL. Enter expressions or module definitions. Ctrl+C to exit.");
			var frontend = new Frontend();
			LoadPrelude(frontend);
			var evaluator = new Evaluator();
			LoadBuiltins(evaluator);

			while (true)
			{
				Console.Write(">> ");
				string? line = Console.ReadLine();
				if (line == null) break;
				if (string.IsNullOrWhiteSpace(line)) continue;
				string source = line;
				// If the user explicitly ends with ';;', strip and submit
				if (source.Contains(";;"))
				{
					source = source.Replace(";;", "");
				}
				else
				{
					// Enter continuation mode only if delimiters are unbalanced or the line ends with '\\'
					bool NeedsMore(string s)
					{
						int par = 0, br = 0, brk = 0;
						foreach (var ch in s)
						{
							if (ch == '(') par++; else if (ch == ')') par--;
							else if (ch == '{') br++; else if (ch == '}') br--;
							else if (ch == '[') brk++; else if (ch == ']') brk--;
						}
						if (par != 0 || br != 0 || brk != 0) return true;
						return s.TrimEnd().EndsWith("\\");
					}
					if (NeedsMore(source))
					{
						var buffer = new System.Text.StringBuilder();
						buffer.AppendLine(source);
						while (true)
						{
							Console.Write(".. ");
							var next = Console.ReadLine();
							if (next == null) break;
							if (next.Trim() == ";;") { break; }
							buffer.AppendLine(next);
							if (!NeedsMore(buffer.ToString())) break;
						}
						source = buffer.ToString();
						if (source.Contains(";;")) source = source.Replace(";;", "");
					}
				}
				try
				{
					var (decls, modules, expr, exprType) = frontend.Compile(source);
					if (verbose)
					{
						foreach (var d in decls)
						{
							var dstr = AstPrinter.Print(d);
							Console.WriteLine($"AST decl: {dstr}");
						}
					}
					evaluator.LoadModules(modules);
					evaluator.LoadAdts(modules);
					evaluator.LoadExceptions(modules);
					evaluator.ApplyOpen(decls);
					// Handle top-level val declarations first
					var valTypes = frontend.InferTopVals(modules, decls);
					Value? lastVal = evaluator.ApplyValDecls(decls);
					for (int i = 0; i < valTypes.Count; i++)
					{
						var (name, ty) = valTypes[i];
						evaluator.GlobalEnv.TryGet(name, out var vv);
						Console.WriteLine($"val {name} : {ty} = {vv}");
					}
					if (expr != null)
					{
						if (verbose)
						{
							var ast = AttoML.Core.Parsing.AstPrinter.Print(expr);
							Console.WriteLine($"AST: {ast}");
						}
						var v = evaluator.Eval(expr, evaluator.GlobalEnv);
						evaluator.GlobalEnv.Set("it", v);
						var typeStr = exprType?.ToString() ?? "<unknown>";
						Console.WriteLine($"val it : {typeStr} = {v}");
						if (exprType != null)
						{
							frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), exprType));
						}
					}
					else if (lastVal != null)
					{
						if (valTypes.Count > 0)
						{
							var (name, ty) = valTypes[^1];
							Console.WriteLine($"val it : {ty} = {lastVal}");
							frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), ty));
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"error: {ex.Message}");
				}
			}
		}

		static void LoadBuiltins(Evaluator evaluator)
		{
			var baseMod = BaseModule.Build();
			evaluator.Modules["Base"] = baseMod;
			foreach (var kv in baseMod.Members)
			{
				evaluator.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
			}
			var mathMod = MathModule.Build();
			evaluator.Modules["Math"] = mathMod;
			foreach (var kv in mathMod.Members)
			{
				evaluator.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
			}
			var listMod = ListModule.Build();
			evaluator.Modules["List"] = listMod;
			foreach (var kv in listMod.Members)
			{
				evaluator.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
			}
			var stringMod = StringModule.Build();
			evaluator.Modules["String"] = stringMod;
			foreach (var kv in stringMod.Members)
			{
				evaluator.GlobalEnv.Set($"String.{kv.Key}", kv.Value);
			}
			// Also open Base by default for convenience
			foreach (var kv in baseMod.Members)
			{
				evaluator.GlobalEnv.Set(kv.Key, kv.Value);
			}
			// Optionally open List by default
			foreach (var kv in listMod.Members)
			{
				evaluator.GlobalEnv.Set(kv.Key, kv.Value);
			}
			// Do not open String by default; prefer qualified usage

			// Built-in exception constructor: Fail : string -> exn
			evaluator.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
			// Built-in exception constructors: Div, Domain
			evaluator.GlobalEnv.Set("Div", new AdtVal("Div", null));
			evaluator.GlobalEnv.Set("Domain", new AdtVal("Domain", null));
		}

		static void LoadPrelude(Frontend frontend)
		{
			try
			{
				void LoadOne(string filename)
				{
					var abs = Path.Combine(AppContext.BaseDirectory, "Prelude", filename);
					string src;
					if (File.Exists(abs))
					{
						src = File.ReadAllText(abs);
					}
					else
					{
						var repoRel = Path.Combine(Directory.GetCurrentDirectory(), "src", "AttoML.Interpreter", "Prelude", filename);
						src = File.Exists(repoRel) ? File.ReadAllText(repoRel) : "";
					}
					if (!string.IsNullOrWhiteSpace(src))
					{
						var (pDecls, pModules, _, _) = frontend.Compile(src);
					}
				}
				LoadOne("Complex.atto");
				LoadOne("SymCalc.atto");
			}
			catch { /* ignore prelude load errors */ }
		}
	}
}
