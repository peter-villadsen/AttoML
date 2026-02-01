using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class EvalTests : AttoMLTestBase
    {
        [Fact]
        public void EvaluatesLetIn()
        {
            var src = "let x = 1 in x";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void EvaluatesClosure()
        {
            var src = "let id = fun x -> x in id 42";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void UsesBaseModuleAdd()
        {
            var src = "Base.add 2 3";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(5, ((IntVal)v).Value);
        }
    }
}
