using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.DataStructures
{
    public class ListRecordAdtTests : AttoMLTestBase
    {
        [Fact]
        public void ParsesAndInfersEmptyList()
        {
            var (_, _, _, type) = CompileAndInitialize("[]");
            Assert.NotNull(type);
        }

        [Fact]
        public void EvaluatesRecordLiteral()
        {
            var (_, ev, expr, _) = CompileAndInitialize("{ x = 1, y = true }");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<RecordVal>(v);
        }

        [Fact]
        public void ConstructsSimpleAdt()
        {
            var (_, ev, expr, _) = CompileAndInitialize("type Option = Some of int | None\nSome 3");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsAssignableFrom<Value>(v);
        }
    }
}
