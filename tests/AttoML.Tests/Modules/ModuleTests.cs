using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Modules
{
    public class ModuleTests : AttoMLTestBase
    {
        [Fact]
        public void DefinesStructureAndOpens()
        {
            var src = "structure M = { let x = 10 }\nopen M\nx";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        [Fact]
        public void UsesMathSin()
        {
            var src = "Math.sin 0.0";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(0.0, ((FloatVal)v).Value, 6);
        }
    }
}
