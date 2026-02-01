using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ComplexModuleTests : AttoMLTestBase
    {
        private static string LoadComplexPrelude()
        {
            var baseDir = System.AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var path = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "Complex.atto");
            return System.IO.File.ReadAllText(path);
        }

        [Fact]
        public void ComplexAdd_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadComplexPrelude() + "\nopen Complex\nadd (C (1.0, 2.0)) (C (3.0, 4.0))");
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("C", av.Ctor);
            var t = Assert.IsType<TupleVal>(av.Payload);
            Assert.Equal(1.0 + 3.0, ((FloatVal)t.Items[0]).Value);
            Assert.Equal(2.0 + 4.0, ((FloatVal)t.Items[1]).Value);
        }

        [Fact]
        public void ComplexMul_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadComplexPrelude() + "\nopen Complex\nmul (C (2.0, 3.0)) (C (4.0, 5.0))");
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            var t = Assert.IsType<TupleVal>(av.Payload);
            Assert.Equal(-7.0, ((FloatVal)t.Items[0]).Value);
            Assert.Equal(22.0, ((FloatVal)t.Items[1]).Value);
        }

        [Fact]
        public void ComplexConj_MagnitudeSquared_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadComplexPrelude() + "\nopen Complex\nlet c = C (3.0, 0.0 - 4.0) in magnitudeSquared (conj c)");
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var fv = Assert.IsType<FloatVal>(v);
            Assert.Equal(25.0, fv.Value);
        }
    }
}
