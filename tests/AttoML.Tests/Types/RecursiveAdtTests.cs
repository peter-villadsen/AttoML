using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Types
{
    public class RecursiveAdtTests : AttoMLTestBase
    {
        [Fact]
        public void DefineRecursiveExprType_AndConstructValue()
        {
            var src = "type Expr = Constant of int | Add of Expr * Expr | Subtract of Expr * Expr\nval e = Add (Constant 1, Subtract (Constant 2, Constant 3))";
            var (fe, ev, decls, mods, _, _) = CompileAndInitializeFull(src);
            fe.InferTopVals(mods, decls);
            ev.ApplyValDecls(decls);
            Assert.True(ev.GlobalEnv.TryGet("e", out var evv));
            var e = Assert.IsType<AdtVal>(evv);
            Assert.Equal("Add", e.Ctor);
            var tuple = Assert.IsType<TupleVal>(e.Payload);
            var left = Assert.IsType<AdtVal>(tuple.Items[0]);
            Assert.Equal("Constant", left.Ctor);
            var leftPayload = Assert.IsType<IntVal>(left.Payload);
            Assert.Equal(1, leftPayload.Value);
        }
    }
}
