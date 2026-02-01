using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class TopLevelValTests : AttoMLTestBase
    {
        [Fact]
        public void Factorial_Computes120()
        {
            var src = "let rec fact n = if n = 0 then 1 else n * fact (n - 1) in fact 5";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(120, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelVal_BindsAndUsable()
        {
            var (fe, ev, decls1, mods1, _, _) = CompileAndInitializeFull("val x = 2");
            fe.InferTopVals(mods1, decls1);
            ev.ApplyValDecls(decls1);

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
            var (_, ev, decls, _, _, _) = CompileAndInitializeFull(src);
            var last = ev.ApplyValDecls(decls);
            Assert.NotNull(last);
            Assert.True(ev.GlobalEnv.TryGet("it", out var itv));
            Assert.IsType<IntVal>(itv);
            Assert.Equal(7, ((IntVal)itv!).Value);
        }

        [Fact]
        public void TopLevelVal_WithAnnotation_Succeeds()
        {
            var (fe, ev, decls1, mods1, _, _) = CompileAndInitializeFull("val x : int = 1");
            fe.InferTopVals(mods1, decls1);
            ev.ApplyValDecls(decls1);

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
    }
}
