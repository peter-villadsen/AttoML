using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class RecursiveAdtTests
    {
        [Fact]
        public void DefineRecursiveExprType_AndConstructValue()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var src = "type Expr = Constant of int | Add of Expr * Expr | Subtract of Expr * Expr\nval e = Add (Constant 1, Subtract (Constant 2, Constant 3))";
            var (decls, mods, expr, type) = fe.Compile(src);
            fe.InferTopVals(mods, decls);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
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
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
        }
    }
}
