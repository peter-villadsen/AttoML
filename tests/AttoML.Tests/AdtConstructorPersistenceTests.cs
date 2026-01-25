using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class AdtConstructorPersistenceTests
    {
        [Fact]
        public void AdtConstructor_PersistsAcrossReplInputs()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            // First input: define ADT type
            {
                var (decls1, mods1, expr1, type1) = fe.Compile("type Banana = Const of int");
                ev.LoadModules(mods1);
                ev.LoadAdts(mods1);
                // No expr to evaluate here
            }

            // Second input: use constructor in a top-level val
            var (decls2, mods2, expr2, type2) = fe.Compile("val q = Const 22");
            fe.InferTopVals(mods2, decls2);
            ev.LoadModules(mods2);
            ev.ApplyValDecls(decls2);
            Assert.True(ev.GlobalEnv.TryGet("q", out var qv));
            var q = Assert.IsType<AdtVal>(qv);
            Assert.Equal("Const", q.Ctor);
            var payload = Assert.IsType<IntVal>(q.Payload);
            Assert.Equal(22, payload.Value);
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
