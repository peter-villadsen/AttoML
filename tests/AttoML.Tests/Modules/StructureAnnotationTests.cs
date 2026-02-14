using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Modules
{
    public class StructureAnnotationTests : AttoMLTestBase
    {
        [Fact]
        public void StructureAnnotatedBinding_Succeeds()
        {
            var src = "structure M = { let x : int = 10 }\nopen M\nx";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureAnnotatedBinding_Mismatch_Fails()
        {
            var src = "structure M = { let x : int = true }";
            var fe = new Frontend();
            Assert.Throws<AttoML.Core.TypeException>(() => fe.Compile(src));
        }
    }
}
