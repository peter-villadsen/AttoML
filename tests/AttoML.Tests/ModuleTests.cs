using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void DefinesStructureAndOpens()
        {
            var src = "structure M = { let x = 10 }\nopen M\nx";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        [Fact]
        public void UsesMathSin()
        {
            var src = "Math.sin 0.0";
            var fe = new Frontend();
            var (decls2, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(0.0, ((FloatVal)v).Value, 6);
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
