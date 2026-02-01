using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class FunSugarTests : AttoMLTestBase
    {
        [Fact]
        public void TopLevelFunSugar_WorksForTwoParams()
        {
            var (fe, ev, decls1, mods1, _, _) = CompileAndInitializeFull("fun add x y = x + y");
            fe.InferTopVals(mods1, decls1);
            ev.ApplyValDecls(decls1);

            var (decls2, mods2, expr2, type2) = fe.Compile("add 1 2");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelFunSugar_AllowsTuplePatternParam()
        {
            var (fe, ev, decls1, mods1, _, _) = CompileAndInitializeFull("fun addPair (x, y) = x + y");
            fe.InferTopVals(mods1, decls1);
            ev.ApplyValDecls(decls1);

            var (decls2, mods2, expr2, type2) = fe.Compile("addPair (1, 2)");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }
    }
}
