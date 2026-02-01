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
            var (decls, mods, expr, type) = frontend.Compile(source);
            var evaluator = new Evaluator();
            LoadBuiltins(evaluator);
            evaluator.LoadModules(mods);
            evaluator.LoadAdts(mods);
            return (frontend, evaluator, expr, type);
        }

        protected (Frontend frontend, Evaluator evaluator, List<ModuleDecl> decls, ModuleSystem mods, Expr? expr, AttoML.Core.Types.Type? type) CompileAndInitializeFull(string source)
        {
            var frontend = new Frontend();
            var (decls, mods, expr, type) = frontend.Compile(source);
            var evaluator = new Evaluator();
            LoadBuiltins(evaluator);
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
        }
    }
}
