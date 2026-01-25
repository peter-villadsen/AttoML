using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class TopLevelValTests
    {
        [Fact]
        public void Factorial_Computes120()
        {
            var src = "let rec fact n = if n = 0 then 1 else n * fact (n - 1) in fact 5";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(120, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelVal_BindsAndUsable()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            // First input: define top-level val
            {
                var (decls1, mods1, expr1, type1) = fe.Compile("val x = 2");
                fe.InferTopVals(mods1, decls1);
                ev.LoadModules(mods1);
                ev.ApplyValDecls(decls1);
            }
            // Second input: use it in a separate expression
            var (decls2, mods2, expr2, type2) = fe.Compile("x + 3");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(5, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelVal_UpdatesIt()
        {
            var src = "val y = 7";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            var last = ev.ApplyValDecls(decls);
            Assert.NotNull(last);
            Assert.True(ev.GlobalEnv.TryGet("it", out var itv));
            Assert.IsType<IntVal>(itv);
            Assert.Equal(7, ((IntVal)itv!).Value);
        }

        [Fact]
        public void TopLevelVal_WithAnnotation_Succeeds()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            // Define annotated val
            {
                var (decls1, mods1, expr1, type1) = fe.Compile("val x : int = 1");
                fe.InferTopVals(mods1, decls1);
                ev.LoadModules(mods1);
                ev.ApplyValDecls(decls1);
            }
            // Use it
            var (decls2, mods2, expr2, type2) = fe.Compile("x");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelVal_WithAnnotationMismatch_Fails()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("val z : int = true");
            Assert.Throws<System.Exception>(() => fe.InferTopVals(mods, decls));
        }

        private static void Program_LoadBuiltins(Evaluator ev)
        {
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
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
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
        }
    }
}
