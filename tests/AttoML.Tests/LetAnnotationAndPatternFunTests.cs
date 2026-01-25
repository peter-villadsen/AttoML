using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class LetAnnotationAndPatternFunTests
    {
        [Fact]
        public void LetAnnotation_SucceedsAndUsed()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("let x : int = 41 in x + 1");
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void LetAnnotation_Mismatch_Fails()
        {
            var fe = new Frontend();
            Assert.Throws<System.Exception>(() => fe.Compile("let x : int = true in x"));
        }

        [Fact]
        public void FunTupleParam_Works()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("(fun (x, y) -> x + y) (2, 3)");
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(5, ((IntVal)v).Value);
        }

        [Fact]
        public void Precedence_ArithmeticThenRelational()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("1 + 2 * 3 < 10");
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<BoolVal>(v);
            Assert.True(((BoolVal)v).Value);
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
