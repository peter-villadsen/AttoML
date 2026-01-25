using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class PatternMatchingTests
    {
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
        }

        [Fact]
        public void MatchesTuplePatternBindsVar()
        {
            var src = "match (1, true) with (x, _) -> x";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void AdtMatchOptionReturnsPayload()
        {
            var src = "type Option = None | Some of int\nlet f = fun o -> match o with None -> 0 | Some x -> x in f (Some 3)";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
            // Type inference should yield int
            Assert.NotNull(type);
        }

        [Fact]
        public void TypeInferenceUnifiesBranchTypes()
        {
            var src = "type T = A | B\nmatch A with A -> 1 | B -> 2";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            Assert.NotNull(type);
            // We cannot easily inspect structure; rely on ToString containing 'int'
            Assert.Contains("int", type!.ToString());
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void NonExhaustiveMatchThrows()
        {
            var src = "type Option = None | Some of int\nmatch Some 1 with None -> 0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            Assert.Throws<Exception>(() => ev.Eval(expr!, ev.GlobalEnv));
        }
    }
}
