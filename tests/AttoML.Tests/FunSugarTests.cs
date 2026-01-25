using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class FunSugarTests
    {
        [Fact]
        public void TopLevelFunSugar_WorksForTwoParams()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            // Define function via sugar
            {
                var (decls1, mods1, expr1, type1) = fe.Compile("fun add x y = x + y");
                fe.InferTopVals(mods1, decls1);
                ev.LoadModules(mods1);
                ev.ApplyValDecls(decls1);
            }

            // Use it
            var (decls2, mods2, expr2, type2) = fe.Compile("add 1 2");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void TopLevelFunSugar_AllowsTuplePatternParam()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            // Define function via sugar with tuple pattern
            {
                var (decls1, mods1, expr1, type1) = fe.Compile("fun addPair (x, y) = x + y");
                fe.InferTopVals(mods1, decls1);
                ev.LoadModules(mods1);
                ev.ApplyValDecls(decls1);
            }

            // Use it
            var (decls2, mods2, expr2, type2) = fe.Compile("addPair (1, 2)");
            ev.LoadModules(mods2);
            var v = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
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
