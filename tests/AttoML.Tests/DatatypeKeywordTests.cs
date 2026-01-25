using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class DatatypeKeywordTests
    {
        [Fact]
        public void Datatype_DefinesAdt_ConstructorsUsable()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("datatype Option = Some of int | None\nSome 3");
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("Some", av.Ctor);
            var payload = Assert.IsType<IntVal>(av.Payload);
            Assert.Equal(3, payload.Value);
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
