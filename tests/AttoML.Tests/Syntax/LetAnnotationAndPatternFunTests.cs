using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Syntax
{
    public class LetAnnotationAndPatternFunTests : AttoMLTestBase
    {
        [Fact]
        public void LetAnnotation_SucceedsAndUsed()
        {
            var (_, ev, expr, _) = CompileAndInitialize("let x : int = 41 in x + 1");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void LetAnnotation_Mismatch_Fails()
        {
            var fe = new Frontend();
            Assert.Throws<System.Exception>(() => fe.Compile("let x : int = true in x"));
        }

        [Fact]
        public void FunTupleParam_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("(fun (x, y) -> x + y) (2, 3)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(5, ((IntVal)v).Value);
        }

        [Fact]
        public void Precedence_ArithmeticThenRelational()
        {
            var (_, ev, expr, _) = CompileAndInitialize("1 + 2 * 3 < 10");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<BoolVal>(v);
            Assert.True(((BoolVal)v).Value);
        }
    }
}
