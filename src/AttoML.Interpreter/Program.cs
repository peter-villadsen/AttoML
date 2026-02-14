using System;
using System.IO;
using AttoML.Core;
using AttoML.Core.Parsing;
using AttoML.Core.Types;
using AttoML.Interpreter.Builtins;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter
{
	internal class Program
	{
		static void Main(string[] args)
		{
			bool verbose = false;
			bool forceRepl = false;
			string? path = null;
			for (int i = 0; i < args.Length; i++)
			{
				var a = args[i];
				var aLower = a.ToLowerInvariant();
				if (aLower == "-v" || aLower == "--verbose") 
				{ 
					verbose = true; 
					continue; 
				}
				
				if (aLower == "-r" || aLower == "--repl") 
				{ 
					forceRepl = true; 
					continue; 
				}
				
				if (!a.StartsWith("-")) 
				{ 
					path = a; 
					break; 
				}
			}

			if (forceRepl || path == null)
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
			var evaluator = new Evaluator();
			LoadBuiltins(evaluator);
			LoadPrelude(frontend, evaluator, verbose);
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
			
			// Handle 'open' declarations by injecting unqualified names into BaseTypeEnv
			foreach (var d in decls)
			{
				if (d is OpenDecl od)
				{
					if (verbose) Console.WriteLine($"[FILE] Processing open {od.Name} - injecting unqualified names into BaseTypeEnv");
					// Find all qualified names starting with "ModuleName."
					var prefix = od.Name + ".";
					var qualifiedNames = frontend.BaseTypeEnv.Names.Where(n => n.StartsWith(prefix)).ToList();
					if (verbose) Console.WriteLine($"[FILE]   Found {qualifiedNames.Count} qualified names for {od.Name}");
					foreach (var qname in qualifiedNames)
					{
						var unqualifiedName = qname.Substring(prefix.Length);
						if (frontend.BaseTypeEnv.TryGet(qname, out var scheme))
						{
							frontend.BaseTypeEnv.Add(unqualifiedName, scheme);
							if (verbose) Console.WriteLine($"[FILE]     Added {unqualifiedName} -> {scheme.Type}");
						}
					}
				}
			}

			// Handle top-level val declarations
			var valTypes = frontend.InferTopVals(modules, decls);
			Value? lastVal = evaluator.ApplyValDecls(decls);
			for (int i = 0; i < valTypes.Count; i++)
			{
				var (name, ty) = valTypes[i];
				evaluator.GlobalEnv.TryGet(name, out var vv);
				Console.WriteLine($"val {name} : {TypePrinter.Print(ty)} = {vv}");
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
				var typeStr = exprType != null ? TypePrinter.Print(exprType) : "<unknown>";
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
					Console.WriteLine($"val it : {TypePrinter.Print(ty)} = {lastVal}");
					frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), ty));
				}
			}
		}

		static void RunRepl(bool verbose)
		{
			Console.WriteLine("AttoML REPL. Enter expressions or module definitions. Ctrl+C to exit.");
			var frontend = new Frontend();
			var evaluator = new Evaluator();
			LoadBuiltins(evaluator);
			LoadPrelude(frontend, evaluator, verbose);

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
					if (verbose) Console.WriteLine($"[REPL] Compiling: {source}");
					var (decls, modules, expr, exprType) = frontend.Compile(source);
					if (verbose)
					{
						Console.WriteLine($"[REPL] Compiled. Decls: {decls.Count}, Expr: {expr != null}");
						foreach (var d in decls)
						{
							var dstr = AstPrinter.Print(d);
							Console.WriteLine($"[REPL]   AST decl: {dstr}");
							if (d is OpenDecl od)
							{
								Console.WriteLine($"[REPL]   Opening module: {od.Name}");
								Console.WriteLine($"[REPL]   Available in evaluator.Modules: {string.Join(", ", evaluator.Modules.Keys)}");
							}
						}
					}
					evaluator.LoadModules(modules);
					evaluator.LoadAdts(modules);
					evaluator.LoadExceptions(modules);
					evaluator.ApplyOpen(decls);
					
					// Handle 'open' declarations by injecting unqualified names into BaseTypeEnv
					foreach (var d in decls)
					{
						if (d is OpenDecl od)
						{
							if (verbose) Console.WriteLine($"[REPL] Processing open {od.Name} - injecting unqualified names into BaseTypeEnv");
							// Find all qualified names starting with "ModuleName."
							var prefix = od.Name + ".";
							var qualifiedNames = frontend.BaseTypeEnv.Names.Where(n => n.StartsWith(prefix)).ToList();
							if (verbose) Console.WriteLine($"[REPL]   Found {qualifiedNames.Count} qualified names for {od.Name}");
							foreach (var qname in qualifiedNames)
							{
								var unqualifiedName = qname.Substring(prefix.Length);
								if (frontend.BaseTypeEnv.TryGet(qname, out var scheme))
								{
									frontend.BaseTypeEnv.Add(unqualifiedName, scheme);
									if (verbose) Console.WriteLine($"[REPL]     Added {unqualifiedName} -> {scheme.Type}");
								}
							}
						}
					}

					if (verbose && decls.Any(d => d is OpenDecl))
					{
						Console.WriteLine($"[REPL] After processing open declarations, checking for unqualified names:");
						if (frontend.BaseTypeEnv.TryGet("testAdd", out var testAddScheme))
						{
							Console.WriteLine($"[REPL]   testAdd found: {testAddScheme.Type}");
						}
						else
						{
							Console.WriteLine($"[REPL]   testAdd NOT found!");
						}
					}
					
					// Handle top-level val declarations first
					var valTypes = frontend.InferTopVals(modules, decls);
					Value? lastVal = evaluator.ApplyValDecls(decls);
					for (int i = 0; i < valTypes.Count; i++)
					{
						var (name, ty) = valTypes[i];
						evaluator.GlobalEnv.TryGet(name, out var vv);
						Console.WriteLine($"val {name} : {TypePrinter.Print(ty)} = {vv}");
					}
					if (expr != null)
					{
						if (verbose)
						{
							var ast = AttoML.Core.Parsing.AstPrinter.Print(expr);
							Console.WriteLine($"[REPL] Evaluating AST: {ast}");
						}
						var v = evaluator.Eval(expr, evaluator.GlobalEnv);
						evaluator.GlobalEnv.Set("it", v);
						var typeStr = exprType != null ? TypePrinter.Print(exprType) : "<unknown>";
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
							Console.WriteLine($"val it : {TypePrinter.Print(ty)} = {lastVal}");
							frontend.BaseTypeEnv.Add("it", new AttoML.Core.Types.Scheme(System.Array.Empty<AttoML.Core.Types.TVar>(), ty));
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"error: {ex.Message}");
					if (verbose)
					{
						Console.WriteLine($"[REPL] Exception stack trace:");
						Console.WriteLine(ex.StackTrace);
					}
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
			var tupleMod = TupleModule.Build();
			evaluator.Modules["Tuple"] = tupleMod;
			foreach (var kv in tupleMod.Members)
			{
				evaluator.GlobalEnv.Set($"Tuple.{kv.Key}", kv.Value);
			}
			var setMod = SetModule.Build();
			evaluator.Modules["Set"] = setMod;
			foreach (var kv in setMod.Members)
			{
				evaluator.GlobalEnv.Set($"Set.{kv.Key}", kv.Value);
			}
			// MapImplementation module (low-level, internal)
			// The high-level Map wrapper is loaded from Prelude/Map.atto
			var mapImplMod = MapImplementationModule.Build();
			evaluator.Modules["MapImplementation"] = mapImplMod;
			foreach (var kv in mapImplMod.Members)
			{
				evaluator.GlobalEnv.Set($"MapImplementation.{kv.Key}", kv.Value);
			}
			// TextIOImplementation module (low-level, internal)
			// The high-level TextIO wrapper is loaded from Prelude/TextIO.atto
			var textIOImplMod = TextIOImplementationModule.Build();
			evaluator.Modules["TextIOImplementation"] = textIOImplMod;
			foreach (var kv in textIOImplMod.Members)
			{
				evaluator.GlobalEnv.Set($"TextIOImplementation.{kv.Key}", kv.Value);
			}
			// HTTP module
			var httpMod = HttpModule.Build();
			evaluator.Modules["Http"] = httpMod;
			foreach (var kv in httpMod.Members)
			{
				evaluator.GlobalEnv.Set($"Http.{kv.Key}", kv.Value);
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

		static void LoadPrelude(Frontend frontend, Evaluator evaluator, bool verbose)
		{
			void LoadOne(string filename)
			{
				var abs = Path.Combine(AppContext.BaseDirectory, "Prelude", filename);
				string src;
				if (File.Exists(abs))
				{
					if (verbose) Console.WriteLine($"[PRELUDE] Found {filename} at: {abs}");
					src = File.ReadAllText(abs);
				}
				else
				{
					var repoRel = Path.Combine(Directory.GetCurrentDirectory(), "src", "AttoML.Interpreter", "Prelude", filename);
					if (File.Exists(repoRel))
					{
						if (verbose) Console.WriteLine($"[PRELUDE] Found {filename} at: {repoRel}");
						src = File.ReadAllText(repoRel);
					}
					else
					{
						if (verbose) Console.WriteLine($"[PRELUDE] WARNING: Could not find {filename}");
						if (verbose) Console.WriteLine($"[PRELUDE]   Tried: {abs}");
						if (verbose) Console.WriteLine($"[PRELUDE]   Tried: {repoRel}");
						src = "";
					}
				}
				if (!string.IsNullOrWhiteSpace(src))
				{
					if (verbose) Console.WriteLine($"[PRELUDE] Loading {filename}... (size: {src.Length} bytes)");
					var (pDecls, pModules, _, _) = frontend.Compile(src);
					if (verbose) Console.WriteLine($"[PRELUDE]   Compiled: {pModules.Structures.Count} structures, {pModules.Adts.Count} ADTs");
					
					evaluator.LoadAdts(pModules);
					if (verbose) Console.WriteLine($"[PRELUDE]   Loaded ADTs");
					
					evaluator.LoadModules(pModules);
					if (verbose) Console.WriteLine($"[PRELUDE]   Loaded modules into evaluator. Modules in evaluator.Modules: {string.Join(", ", evaluator.Modules.Keys)}");
					
					// Also need to persist the structure types into BaseTypeEnv for REPL commands
					// Inject structure member types into the base type environment
					var ti = new AttoML.Core.Types.TypeInference();
					var tempEnv = pModules.InjectStructuresInto(frontend.BaseTypeEnv, ti, null);
					
					// Copy the qualified names from tempEnv to BaseTypeEnv
					foreach (var s in pModules.Structures.Values)
					{
						if (verbose) Console.WriteLine($"[PRELUDE]   Processing structure {s.Name} with {s.OrderedBindings.Count} bindings");
						foreach (var (bn, _, _) in s.OrderedBindings)
						{
							var qname = $"{s.Name}.{bn}";
							if (tempEnv.TryGet(qname, out var scheme))
							{
								frontend.BaseTypeEnv.Add(qname, scheme);
								if (verbose) Console.WriteLine($"[PRELUDE]     Added type for {qname}: {scheme.Type}");
							}
							else
							{
								if (verbose) Console.WriteLine($"[PRELUDE]     WARNING: Could not find type for {qname}");
							}
						}
					}

					// CRITICAL FIX: Also copy ADT constructors from tempEnv to BaseTypeEnv
					// so that constructors like Some, None are available in subsequent compilations
					foreach (var adt in pModules.Adts.Values)
					{
						if (verbose) Console.WriteLine($"[PRELUDE]   Registering ADT constructors for {adt.TypeName}");
						foreach (var (ctor, _) in adt.Ctors)
						{
							if (tempEnv.TryGet(ctor, out var ctorScheme))
							{
								frontend.BaseTypeEnv.Add(ctor, ctorScheme);
								if (verbose) Console.WriteLine($"[PRELUDE]     Added ADT constructor {ctor}: {ctorScheme}");
							}
							else
							{
								if (verbose) Console.WriteLine($"[PRELUDE]     WARNING: Could not find type for constructor {ctor}");
							}
						}
					}

					if (verbose) Console.WriteLine($"[PRELUDE]   Completed loading {filename}");
				}
				else
				{
					if (verbose) Console.WriteLine($"[PRELUDE] Skipping {filename} - file is empty or not found");
				}
			}
			LoadOne("Option.atto");
			LoadOne("Result.atto");
			LoadOne("TextIO.atto");
			LoadOne("Map.atto");
			// LoadOne("Parser.atto"); // TODO: Fix parser error with 'open' in structure
			// LoadOne("State.atto");
			// LoadOne("Writer.atto");
			// LoadOne("Complex.atto");
			// LoadOne("SymCalc.atto");
			// LoadOne("EGraph.atto");
			// LoadOne("LaTeXRewrite.atto");
			// LoadOne("LaTeX.atto");
		}
	}
}
