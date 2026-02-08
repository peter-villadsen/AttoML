using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Types
{
    public class AdtConstructorPersistenceTests : AttoMLTestBase
    {
        [Fact]
        public void AdtConstructor_PersistsAcrossReplInputs()
        {
            var (fe, ev, decls1, mods1, _, _) = CompileAndInitializeFull("type Banana = Const of int");

            var (decls2, mods2, expr2, type2) = fe.Compile("val q = Const 22");
            fe.InferTopVals(mods2, decls2);
            ev.LoadModules(mods2);
            ev.ApplyValDecls(decls2);
            Assert.True(ev.GlobalEnv.TryGet("q", out var qv));
            var q = Assert.IsType<AdtVal>(qv);
            Assert.Equal("Const", q.Ctor);
            var payload = Assert.IsType<IntVal>(q.Payload);
            Assert.Equal(22, payload.Value);
        }
    }
}
