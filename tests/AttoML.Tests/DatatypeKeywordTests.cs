using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class DatatypeKeywordTests : AttoMLTestBase
    {
        [Fact]
        public void Datatype_DefinesAdt_ConstructorsUsable()
        {
            var (_, ev, expr, _) = CompileAndInitialize("datatype Option = Some of int | None\nSome 3");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("Some", av.Ctor);
            var payload = Assert.IsType<IntVal>(av.Payload);
            Assert.Equal(3, payload.Value);
        }
    }
}
