using System.Collections.Generic;
using AttoML.Core;
using AttoML.Core.Modules;
using AttoML.Core.Parsing;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public abstract class AttoMLTestBase
    {
        protected (Frontend frontend, Evaluator evaluator, Expr? expr, AttoML.Core.Types.Type? type) CompileAndInitialize(string source)
        {
            var frontend = new Frontend();
            LoadPrelude(frontend, evaluator: null!);
            var (decls, mods, expr, type) = frontend.Compile(source);
            var evaluator = new Evaluator();
            LoadBuiltins(evaluator);
            LoadPrelude(frontend, evaluator);
            evaluator.LoadModules(mods);
            evaluator.LoadAdts(mods);
            return (frontend, evaluator, expr, type);
        }

        protected (Frontend frontend, Evaluator evaluator, List<ModuleDecl> decls, ModuleSystem mods, Expr? expr, AttoML.Core.Types.Type? type) CompileAndInitializeFull(string source)
        {
            var frontend = new Frontend();
            LoadPrelude(frontend, evaluator: null!);
            var (decls, mods, expr, type) = frontend.Compile(source);
            var evaluator = new Evaluator();
            LoadBuiltins(evaluator);
            LoadPrelude(frontend, evaluator);
            evaluator.LoadModules(mods);
            evaluator.LoadAdts(mods);
            return (frontend, evaluator, decls, mods, expr, type);
        }

        private static void LoadBuiltins(Evaluator ev)
        {
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
            }
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            var mathMod = AttoML.Interpreter.Builtins.MathModule.Build();
            ev.Modules["Math"] = mathMod;
            foreach (var kv in mathMod.Members)
            {
                ev.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
            }
            var listMod = AttoML.Interpreter.Builtins.ListModule.Build();
            ev.Modules["List"] = listMod;
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
            }
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            var stringMod = AttoML.Interpreter.Builtins.StringModule.Build();
            ev.Modules["String"] = stringMod;
            foreach (var kv in stringMod.Members)
            {
                ev.GlobalEnv.Set($"String.{kv.Key}", kv.Value);
            }
            var tupleMod = AttoML.Interpreter.Builtins.TupleModule.Build();
            ev.Modules["Tuple"] = tupleMod;
            foreach (var kv in tupleMod.Members)
            {
                ev.GlobalEnv.Set($"Tuple.{kv.Key}", kv.Value);
            }
        }

        private static void LoadPrelude(Frontend frontend, Evaluator evaluator)
        {
            void LoadPreludeFile(string filename)
            {
                var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AttoML.Interpreter", "Prelude", filename);
                var src = System.IO.File.ReadAllText(path);
                var (pDecls, pMods, _, _) = frontend.Compile(src);

                if (evaluator != null)
                {
                    evaluator.LoadAdts(pMods);
                    evaluator.LoadModules(pMods);
                }

                // Inject structure member types into BaseTypeEnv for type checking
                var ti = new AttoML.Core.Types.TypeInference();
                var tempEnv = pMods.InjectStructuresInto(frontend.BaseTypeEnv, ti, null);

                // Copy the qualified names from tempEnv to BaseTypeEnv
                foreach (var s in pMods.Structures.Values)
                {
                    foreach (var (bn, _, _) in s.OrderedBindings)
                    {
                        var qname = $"{s.Name}.{bn}";
                        if (tempEnv.TryGet(qname, out var scheme))
                        {
                            frontend.BaseTypeEnv.Add(qname, scheme);
                        }
                    }
                }
            }

            LoadPreludeFile("Option.atto");
            LoadPreludeFile("Result.atto");
        }
    }
}
