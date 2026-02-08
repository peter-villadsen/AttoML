using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Syntax
{
    public class InfixOperatorsTests : AttoMLTestBase
    {
        [Fact]
        public void FunWithPlusParsesAndEvaluates()
        {
            var (_, ev, expr, _) = CompileAndInitialize("(fun x -> x + 1) 41");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void RelationalOperators()
        {
            var (fe, ev, _, _) = CompileAndInitialize("1 < 2");
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
            var src = "false andthen Base.eq (Base.div 1 0) 0";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.False(((BoolVal)v).Value);
        }

        [Fact]
        public void ShortCircuitOrElse()
        {
            var src = "true orelse Base.eq (Base.div 1 0) 0";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }
    }
}
