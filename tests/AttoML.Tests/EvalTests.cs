using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class EvalTests
    {
        [Fact]
        public void EvaluatesLetIn()
        {
            var src = "let x = 1 in x";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void EvaluatesClosure()
        {
            var src = "let id = fun x -> x in id 42";
            var fe = new Frontend();
            var (decls2, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void UsesBaseModuleAdd()
        {
            var src = "Base.add 2 3";
            var fe = new Frontend();
            var (decls3, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(5, ((IntVal)v).Value);
        }

        private static void Program_LoadBuiltins(Evaluator ev)
        {
            // Mimic Program.LoadBuiltins without reflection
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
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
        }
    }
}
