using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class InfixOperatorsTests
    {
        private (Frontend, Evaluator) Setup()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members) { ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value); ev.GlobalEnv.Set(kv.Key, kv.Value); }
            return (fe, ev);
        }

        [Fact]
        public void FunWithPlusParsesAndEvaluates()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("(fun x -> x + 1) 41");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void RelationalOperators()
        {
            var (fe, ev) = Setup();
            Assert.True(((BoolVal)ev.Eval(fe.Compile("1 < 2").expr!, ev.GlobalEnv)).Value);
            Assert.True(((BoolVal)ev.Eval(fe.Compile("2 > 1").expr!, ev.GlobalEnv)).Value);
            Assert.True(((BoolVal)ev.Eval(fe.Compile("2 >= 2").expr!, ev.GlobalEnv)).Value);
            Assert.True(((BoolVal)ev.Eval(fe.Compile("1 <= 2").expr!, ev.GlobalEnv)).Value);
            Assert.True(((BoolVal)ev.Eval(fe.Compile("1 = 1").expr!, ev.GlobalEnv)).Value);
            Assert.True(((BoolVal)ev.Eval(fe.Compile("1 <> 2").expr!, ev.GlobalEnv)).Value);
        }

        [Fact]
        public void ShortCircuitAndThen()
        {
            var (fe, ev) = Setup();
            // RHS would crash if evaluated; ensure not evaluated when LHS is false
            var src = "false andthen Base.eq (Base.div 1 0) 0";
            var (decls, mods, expr, type) = fe.Compile(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.False(((BoolVal)v).Value);
        }

        [Fact]
        public void ShortCircuitOrElse()
        {
            var (fe, ev) = Setup();
            var src = "true orelse Base.eq (Base.div 1 0) 0";
            var (decls, mods, expr, type) = fe.Compile(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }
    }
}
