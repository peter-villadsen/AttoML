using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class OptionTypeTests
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
        public void Datatype_DefinesOption_ConstructorsUsable()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("datatype option = NONE | SOME of int\nSOME 3");
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("SOME", av.Ctor);
            var payload = Assert.IsType<IntVal>(av.Payload);
            Assert.Equal(3, payload.Value);
        }

        [Fact]
        public void MatchOptionExtractsPayload()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            LoadBuiltins(ev);

            var src = "datatype option = NONE | SOME of int\nlet f = fun o -> match o with NONE -> 0 | SOME x -> x in f (SOME 42)";
            var (decls, mods, expr, type) = fe.Compile(src);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(42, Assert.IsType<IntVal>(v).Value);
        }

        [Fact]
        public void NonExhaustiveMatchThrowsOnSOME()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            LoadBuiltins(ev);

            var src = "datatype option = NONE | SOME of int\nmatch SOME 1 with NONE -> 0";
            var (decls, mods, expr, type) = fe.Compile(src);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            Assert.Throws<Exception>(() => ev.Eval(expr!, ev.GlobalEnv));
        }

        [Fact]
        public void QualifiedConstructorPatternsWork()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            LoadBuiltins(ev);

            var src = "datatype option = NONE | SOME of int\nmatch option.SOME 2 with option.NONE -> 0 | option.SOME x -> x";
            var (decls, mods, expr, type) = fe.Compile(src);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(2, Assert.IsType<IntVal>(v).Value);
        }

        [Fact]
        public void OptionMapExample()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            LoadBuiltins(ev);

                        var src = @"datatype option = NONE | SOME of int
structure Option = {
    let map = fun f -> fun o ->
        match o with NONE -> NONE | SOME x -> SOME (f x)
}
let inc = fun x -> x + 1 in Option.map inc (SOME 4)";
            var (decls, mods, expr, type) = fe.Compile(src);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("SOME", av.Ctor);
            Assert.Equal(5, Assert.IsType<IntVal>(av.Payload).Value);
        }
    }
}
