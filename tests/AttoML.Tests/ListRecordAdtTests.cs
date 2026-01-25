using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ListRecordAdtTests
    {
        [Fact]
        public void ParsesAndInfersEmptyList()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("[]");
            Assert.NotNull(type);
        }

        [Fact]
        public void EvaluatesRecordLiteral()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("{ x = 1, y = true }");
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<RecordVal>(v);
        }

        [Fact]
        public void ConstructsSimpleAdt()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile("type Option = Some of int | None\nSome 3");
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsAssignableFrom<Value>(v);
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
